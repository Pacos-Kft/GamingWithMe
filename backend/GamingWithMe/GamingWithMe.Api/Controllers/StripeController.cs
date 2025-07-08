using AutoMapper;
using GamingWithMe.Application.Commands;
using GamingWithMe.Application.Dtos;
using GamingWithMe.Application.Interfaces;
using GamingWithMe.Application.Queries;
using GamingWithMe.Domain.Entities;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using Microsoft.Identity.Client;
using Stripe;
using Stripe.Checkout;
using System.Security.Claims;
using System.Text.Json;

namespace GamingWithMe.Api.Controllers
{
    public class PaymentRequest
    {
        public string PriceId { get; set; }
        public BookingCommand BookingDetails { get; set; }
    }


    [Route("api/[controller]")]
    [ApiController]
    public class StripeController : ControllerBase
    {
        private readonly StripeModel _model;
        private readonly TokenService _token;
        private readonly CustomerService _customerService;
        private readonly ChargeService _charge;
        private readonly ProductService _product;
        private readonly PriceService _priceService;
        private readonly IMediator _mediator;
        private readonly string _webhookSecret;
        private readonly IAsyncRepository<User> _gamerRepo;



        public StripeController(IOptions<StripeModel> model, TokenService token, CustomerService customer, ChargeService charge, ProductService product, IMediator mediator, IAsyncRepository<IdentityUser> repo, IAsyncRepository<User> gamerRepo, PriceService priceService)
        {
            _model = model.Value;
            _token = token;
            _customerService = customer;
            _charge = charge;
            _product = product;
            _mediator = mediator;
            // It's recommended to store your webhook secret in appsettings.json or another secure configuration provider
            _webhookSecret = "whsec_a502365718c2eabc23f204e03d5e5d26f9fc4bc595de1303ea802e992dd86bd3"; // Replace with your actual webhook signing secret
            _priceService = priceService;
            _gamerRepo = gamerRepo;
        }

        [HttpPost("pay/{mentorId}")]
        [Authorize]
        public async Task<IActionResult> Pay(Guid mentorId, [FromBody] BookingDetailsDto request)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);

            if (userId == null)
            {
                return BadRequest("User not found");
            }

            var cmd = new BookingCommand(mentorId, userId,"", request);

            var gamer = await _gamerRepo.GetByIdAsync(mentorId, default, x=> x.Products);

            if (gamer == null) {
                return BadRequest();
            }

            var product = gamer.Products.FirstOrDefault();

            if (product == null) {
                return NotFound();
            }

            var connectedAccount = gamer.StripeAccount;


            try
            {
                var validationCommand = new ValidateBookingCommand(cmd);
                await _mediator.Send(validationCommand);

                StripeConfiguration.ApiKey = _model.SecretKey;


                // Retrieve the actual price information
                var price = _priceService.Get(product.StripePriceId);
                if (price == null)
                    return BadRequest("Invalid price ID");

                // Calculate fee as 5% of the actual price
                long applicationFee = (long)(price.UnitAmount * 0.20);

                var options = new SessionCreateOptions
                {
                    LineItems = new List<SessionLineItemOptions>
                    {
                        new SessionLineItemOptions
                        {
                            Price = product.StripePriceId,
                            Quantity = 1,
                        }
                    },
                    Mode = "payment",
                    SuccessUrl = "http://localhost:5173/success",
                    CancelUrl = "http://localhost:5173",
                    PaymentIntentData = new SessionPaymentIntentDataOptions
                    {
                        ApplicationFeeAmount = applicationFee,
                        TransferData = new SessionPaymentIntentDataTransferDataOptions
                        {
                            Destination = connectedAccount
                        }
                    },
                    Metadata = new Dictionary<string, string>
                    {
                        { "bookingDetails", JsonSerializer.Serialize(cmd) },
                        { "mentorId", mentorId.ToString() },
                        { "userId", userId }
                    }
                };

                //options.Customer = "cus_SaCXeZDWJH5t4L";

                var service = new SessionService();
                Session session = service.Create(options);

                // Return more details for debugging
                return Ok(new
                {
                    CheckoutUrl = session.Url,
                    PriceAmount = price.UnitAmount,
                    ApplicationFee = applicationFee,
                    SessionId = session.Id,
                    ConnectedAccount = connectedAccount
                });
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(ex.Message);
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Error processing payment: {ex.Message}");
            }
        }

        [HttpPost("refund/{bookingId}")]
        [Authorize]
        public async Task<IActionResult> RefundPayment(Guid bookingId)
        {
            try
            {
                StripeConfiguration.ApiKey = _model.SecretKey;

                // 1. Get the booking
                var booking = await _mediator.Send(new GetBookingByIdQuery(bookingId));
                if (booking == null)
                    return NotFound("Booking not found");

                // 2. Check authorization (only the user who booked or the mentor can refund)
                var currentUserId = User.FindFirstValue(ClaimTypes.NameIdentifier);

                var isBookedUser = booking.Customer?.UserId == currentUserId;
                var isGamer = booking.Provider?.UserId == currentUserId;

                if (!isBookedUser && !isGamer)
                    return Forbid("You don't have permission to refund this booking");

                // 3. Check if the booking has a payment ID
                if (string.IsNullOrEmpty(booking.PaymentIntentId))
                    return BadRequest("No payment information associated with this booking");

                // 4. Create refund via Stripe
                var refundService = new RefundService();
                var refundOptions = new RefundCreateOptions
                {
                    PaymentIntent = booking.PaymentIntentId,
                    Reason = RefundReasons.RequestedByCustomer
                };

                var refund = await refundService.CreateAsync(refundOptions);

                // 5. Delete the booking
                await _mediator.Send(new DeleteBookingCommand(bookingId));

                return Ok(new
                {
                    Success = true,
                    RefundId = refund.Id,
                    Amount = refund.Amount,
                    Status = refund.Status
                });
            }
            catch (StripeException ex)
            {
                return StatusCode(StatusCodes.Status400BadRequest, new { Error = ex.Message });
            }
            catch (Exception ex)
            {
                return StatusCode(StatusCodes.Status500InternalServerError, new { Error = ex.Message });
            }
        }

        [HttpPost("webhook")]
        public async Task<IActionResult> Webhook()
        {
            var json = await new StreamReader(HttpContext.Request.Body).ReadToEndAsync();
            try
            {
                var stripeEvent = EventUtility.ConstructEvent(json,
                    Request.Headers["Stripe-Signature"], _webhookSecret);

                // Handle the event
                if (stripeEvent.Type == "checkout.session.completed")
                {
                    var session = stripeEvent.Data.Object as Session;
                    if (session.PaymentStatus == "paid")
                    {
                        // Retrieve booking details from metadata
                        var bookingDetailsJson = session.Metadata["bookingDetails"];
                        var booking = JsonSerializer.Deserialize<BookingCommand>(bookingDetailsJson);

                        var bookingCommand = new BookingCommand(booking.providerId, booking.customerId, session.PaymentIntentId, booking.BookingDetailsDto);


                        // Create the booking
                        await _mediator.Send(bookingCommand);
                    }
                }

                return Ok();
            }
            catch (StripeException e)
            {
                return BadRequest(e.Message);
            }
        }

        [HttpPost("create-product")]
        [Authorize]
        public async Task<IActionResult> CreateProduct([FromBody] NewProductDto productDto)
        {
            StripeConfiguration.ApiKey = _model.SecretKey;

            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);

            if (userId == null)
            {
                return BadRequest("User not found");
            }

            var product = await _mediator.Send(new CreateProductCommand(userId,productDto));

            return Ok(product);
        }

        [HttpPost("create-customer")]
        public async Task<dynamic> CreateCustomer([FromBody] StripeCustomer customer)
        {
            StripeConfiguration.ApiKey = _model.SecretKey;

            var customerOptions = new CustomerCreateOptions
            {
                Email = customer.Email,
                Name = customer.Name,
            };

            var _customer = await _customerService.CreateAsync(customerOptions);

            return new {customer};
        }

        [HttpGet("get-all-products")]
        public IActionResult GetAllProducts() {
            StripeConfiguration.ApiKey = _model.SecretKey;

            var options = new ProductListOptions { Expand = new List<string> { "data.default_price" } };

            var products = _product.List(options);

            return Ok(products);
        }

        [HttpPost("create-connected-account")]
        [AllowAnonymous]
        public async Task<IActionResult> CreateConnectedAccount()
        {
            StripeConfiguration.ApiKey = _model.SecretKey;
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);


            if(userId == null)
            {
                return BadRequest("User not found in controller");
            }

            var (link,account) = await _mediator.Send(new CreateStripeAccountCommand(userId));

            return Ok(new
            {
                OnboardingUrl = link.Url,
                ConnectedAccountId = account.Id 
            });
        }

    }
}
