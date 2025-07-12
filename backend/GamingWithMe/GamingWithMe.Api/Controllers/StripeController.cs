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
    public class CreateCouponRequest
    {
        public string Name { get; set; }
        public decimal PercentOff { get; set; }
        public int DurationInDays { get; set; }
        public int? MaxRedemptions { get; set; }
        public string? CouponId { get; set; }
    }

    public class ApplyCouponRequest
    {
        public string CouponCode { get; set; }
        public string PriceId { get; set; }
    }

    public class PaymentWithCouponRequest
    {
        public Guid AppointmentId { get; set; }
        public string? CouponId { get; set; }
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
        private readonly IAsyncRepository<Domain.Entities.Discount> _discountRepo;



        public StripeController(IOptions<StripeModel> model, TokenService token, CustomerService customer, ChargeService charge, ProductService product, IMediator mediator, IAsyncRepository<IdentityUser> repo, IAsyncRepository<User> gamerRepo, PriceService priceService, IAsyncRepository<Domain.Entities.Discount> discountRepo)
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
            _discountRepo = discountRepo;
        }

        [HttpPost("pay/{mentorId}")]
        [Authorize]
        public async Task<IActionResult> Pay(Guid mentorId, [FromBody] PaymentWithCouponRequest request /*[FromBody] Guid appointmentId*/ /*[FromBody] BookingDetailsDto request*/)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);

            if (userId == null)
            {
                return BadRequest("User not found");
            }

            //var cmd = new BookingCommand(mentorId, userId,"", request);
            var cmd = (mentorId, userId, request.AppointmentId);

            var gamer = await _gamerRepo.GetByIdAsync(mentorId, default, x => x.DailyAvailability, q => q.Discounts);

            if (gamer == null)
            {
                return BadRequest();
            }

            //var product = gamer.Products.FirstOrDefault();

            //if (product == null) {
            //    return NotFound();
            //}

            var connectedAccount = gamer.StripeAccount;

            var schedule = gamer.DailyAvailability.FirstOrDefault(x => x.Id == request.AppointmentId);
            if (schedule == null)
            {
                return BadRequest("Schedule doesnt exist");
            }


            try
            {
                var validationCommand = new ValidateBookingCommand(cmd.mentorId, cmd.userId, cmd.AppointmentId);
                await _mediator.Send(validationCommand);

                StripeConfiguration.ApiKey = _model.SecretKey;


                // Retrieve the actual price information
                //var price = _priceService.Get(product.StripePriceId);
                //if (price == null)
                //    return BadRequest("Invalid price ID");

                // Calculate fee as 10% of the actual price
                var price = schedule.Price * 100;
                long applicationFee = (long)(price * 0.10);

                var options = new SessionCreateOptions
                {
                    LineItems = new List<SessionLineItemOptions>
                    {
                        new SessionLineItemOptions
                        {
                            //Price = product.StripePriceId,
                            //Quantity = 1,
                            PriceData = new SessionLineItemPriceDataOptions
                            {
                                Currency = "usd",
                                ProductData = new SessionLineItemPriceDataProductDataOptions
                                {
                                    Name = "Session"
                                },
                                UnitAmount = price,
                            },
                            Quantity = 1
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

                if (!string.IsNullOrEmpty(request.CouponId))
                {
                    var discount = gamer.Discounts.FirstOrDefault(x => x.StripeId == request.CouponId);

                    if (discount != null)
                    {
                        options.Discounts = new List<SessionDiscountOptions>
                        {
                            new SessionDiscountOptions
                            {
                                Coupon = request.CouponId
                            }
                        };
                    }


                }

                //options.Customer = "cus_SaCXeZDWJH5t4L";

                var service = new SessionService();
                Session session = service.Create(options);

                // Return more details for debugging
                return Ok(new
                {
                    CheckoutUrl = session.Url,
                    PriceAmount = price,
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

                        var bookingCommand = new BookingCommand(booking.providerId, booking.customerId, session.PaymentIntentId, booking.appointmentId);


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

            var product = await _mediator.Send(new CreateProductCommand(userId, productDto));

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

            return new { customer };
        }

        [HttpGet("get-all-products")]
        public IActionResult GetAllProducts()
        {
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


            if (userId == null)
            {
                return BadRequest("User not found in controller");
            }

            var (link, account) = await _mediator.Send(new CreateStripeAccountCommand(userId));

            return Ok(new
            {
                OnboardingUrl = link.Url,
                ConnectedAccountId = account.Id
            });
        }

        [HttpPost("create-coupon")]
        [Authorize]
        public async Task<IActionResult> CreateCoupon([FromBody] CreateCouponRequest request)
        {

            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);

            var user = (await _gamerRepo.ListAsync()).FirstOrDefault(x=> x.UserId == userId);

            if (user == null) {
                return BadRequest("User not found");
            }


            try
            {
                StripeConfiguration.ApiKey = _model.SecretKey;

                var couponOptions = new CouponCreateOptions
                {
                    Name = request.Name,
                    PercentOff = (decimal?)request.PercentOff,
                    Duration = "once", // Can be "forever", "once", or "repeating"
                    Id = request.CouponId ?? null, // Custom ID if provided
                    MaxRedemptions = request.MaxRedemptions
                };

                // Set expiration if duration in days is provided
                if (request.DurationInDays > 0)
                {
                    couponOptions.RedeemBy = DateTime.UtcNow.AddDays(request.DurationInDays);
                }

                var couponService = new CouponService();
                var coupon = await couponService.CreateAsync(couponOptions);

                var myDiscount = new Domain.Entities.Discount
                {
                    StripeId = coupon.Id,
                    Name = coupon.Name,
                    PercentOff = request.PercentOff,
                    Duration = request.DurationInDays,
                    MaxRedemptions = request.MaxRedemptions,
                    UserId = user.Id
                };

                await _discountRepo.AddAsync(myDiscount);

                return Ok(new
                {
                    CouponId = coupon.Id,
                    Name = coupon.Name,
                    PercentOff = coupon.PercentOff,
                    Valid = true,
                    ExpiresAt = coupon.RedeemBy
                });
            }
            catch (StripeException ex)
            {
                return BadRequest(new { Error = ex.Message });
            }
        }

        [HttpGet("validate-coupon/{couponCode}")]
        public async Task<IActionResult> ValidateCoupon(string couponCode)
        {
            try
            {
                StripeConfiguration.ApiKey = _model.SecretKey;

                var couponService = new CouponService();
                var coupon = await couponService.GetAsync(couponCode);

                bool isValid = coupon != null && coupon.Valid;

                return Ok(new
                {
                    Valid = isValid,
                    PercentOff = coupon?.PercentOff,
                    Name = coupon?.Name,
                    ExpiresAt = coupon?.RedeemBy
                });
            }
            catch (StripeException)
            {
                return Ok(new { Valid = false });
            }
        }

        [HttpPost("apply-coupon")]
        public async Task<IActionResult> ApplyCoupon([FromBody] ApplyCouponRequest request)
        {
            try
            {
                StripeConfiguration.ApiKey = _model.SecretKey;

                // Validate the coupon first
                var couponService = new CouponService();
                var coupon = await couponService.GetAsync(request.CouponCode);

                if (coupon == null)
                {
                    return BadRequest(new { Error = "Invalid coupon code" });
                }

                // Validate that the coupon is still valid
                if (!coupon.Valid)
                {
                    return BadRequest(new { Error = "Coupon is expired or maximum redemptions reached" });
                }

                // Rest of the method remains unchanged
                var priceService = new PriceService();
                var price = await priceService.GetAsync(request.PriceId);

                if (price == null)
                {
                    return BadRequest(new { Error = "Invalid price ID" });
                }

                // Calculate discounted price
                var originalAmount = price.UnitAmount ?? 0;
                var discountedAmount = originalAmount;

                if (coupon.PercentOff.HasValue)
                {
                    discountedAmount = originalAmount - (originalAmount * (long)coupon.PercentOff.Value / 100);
                }
                else if (coupon.AmountOff.HasValue)
                {
                    discountedAmount = originalAmount - coupon.AmountOff.Value;
                    if (discountedAmount < 0) discountedAmount = 0;
                }

                return Ok(new
                {
                    CouponCode = coupon.Id,
                    OriginalPrice = originalAmount,
                    DiscountedPrice = discountedAmount,
                    Discount = originalAmount - discountedAmount,
                    PercentOff = coupon.PercentOff
                });
            }
            catch (StripeException ex)
            {
                return BadRequest(new { Error = ex.Message });
            }
        }

    }
}
