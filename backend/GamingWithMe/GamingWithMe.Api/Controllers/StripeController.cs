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
    }

    public class ApplyCouponRequest
    {
        public string CouponCode { get; set; }
        public string PriceId { get; set; }
    }

    // Updated payment request to handle both types
    public class PaymentRequest
    {
        public string PaymentType { get; set; } = "appointment"; // "appointment" or "service"
        public Guid? AppointmentId { get; set; } // For appointment bookings
        public Guid? ServiceId { get; set; } // For service orders
        public string? CouponId { get; set; }
        public string? CustomerNotes { get; set; } // For service orders
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
        private readonly IAsyncRepository<FixedService> _serviceRepo;
        private readonly ILogger<StripeController> _logger;


        public StripeController(IOptions<StripeModel> model, TokenService token, CustomerService customer, ChargeService charge, ProductService product, IMediator mediator, IAsyncRepository<IdentityUser> repo, IAsyncRepository<User> gamerRepo, PriceService priceService, IAsyncRepository<Domain.Entities.Discount> discountRepo, IAsyncRepository<FixedService> serviceRepo, ILogger<StripeController> logger)
        {
            _model = model.Value;
            _token = token;
            _customerService = customer;
            _charge = charge;
            _product = product;
            _mediator = mediator;
            _webhookSecret = _model.WebhookSecret;
            _priceService = priceService;
            _gamerRepo = gamerRepo;
            _discountRepo = discountRepo;
            _serviceRepo = serviceRepo;
            _logger = logger;
        }

        [HttpPost("pay/{providerId}")]
        [Authorize]
        public async Task<IActionResult> Pay(Guid providerId, [FromBody] PaymentRequest request)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);

            if (userId == null)
            {
                return BadRequest("User not found");
            }

            try
            {
                StripeConfiguration.ApiKey = _model.SecretKey;

                var provider = await _gamerRepo.GetByIdAsync(providerId, default, 
                    x => x.DailyAvailability, 
                    q => q.Discounts, 
                    s => s.FixedServices);

                if (provider == null)
                {
                    return BadRequest("Provider not found");
                }

                var connectedAccount = provider.StripeAccount;

                if (string.IsNullOrEmpty(connectedAccount))
                {
                    return BadRequest("Provider hasn't set up payments");
                }

                long price = 0;
                string productName = "";
                string paymentType = request.PaymentType.ToLower();

                if (paymentType == "appointment")
                {
                    if (!request.AppointmentId.HasValue)
                        return BadRequest("AppointmentId is required for appointment bookings");

                    var schedule = provider.DailyAvailability.FirstOrDefault(x => x.Id == request.AppointmentId.Value);
                    if (schedule == null)
                        return BadRequest("Schedule doesn't exist");

                    var validationCommand = new ValidateBookingCommand(providerId, userId, request.AppointmentId.Value);
                    await _mediator.Send(validationCommand);

                    price = schedule.Price * 100;
                    productName = "Gaming Session";
                }
                else if (paymentType == "service")
                {
                    if (!request.ServiceId.HasValue)
                        return BadRequest("ServiceId is required for service orders");

                    var szervice = provider.FixedServices.FirstOrDefault(s => s.Id == request.ServiceId.Value);
                    if (szervice == null)
                        return BadRequest("Service doesn't exist");

                    if (szervice.Status != ServiceStatus.Active)
                        return BadRequest("Service is not available");

                    price = szervice.Price*100;
                    productName = szervice.Title;
                }
                else
                {
                    return BadRequest("PaymentType must be either 'appointment' or 'service'");
                }

                // Calculate fee as 10% of the actual price
                long applicationFee = (long)(price * 0.10);

                var sessionOptions = new SessionCreateOptions
                {
                    LineItems = new List<SessionLineItemOptions>
                    {
                        new SessionLineItemOptions
                        {
                            PriceData = new SessionLineItemPriceDataOptions
                            {
                                Currency = "usd",
                                ProductData = new SessionLineItemPriceDataProductDataOptions
                                {
                                    Name = productName
                                },
                                UnitAmount = price,
                            },
                            Quantity = 1
                        }
                    },
                    Mode = "payment",
                    SuccessUrl = "http://localhost:5173",
                    CancelUrl = "http://localhost:5173",
                    PaymentIntentData = new SessionPaymentIntentDataOptions
                    {
                        ApplicationFeeAmount = applicationFee,
                        TransferData = new SessionPaymentIntentDataTransferDataOptions
                        {
                            Destination = connectedAccount
                        },
                        // Add metadata to PaymentIntent as well
                        Metadata = new Dictionary<string, string>
                        {
                            { "paymentType", paymentType },
                            { "providerId", providerId.ToString() },
                            { "customerId", userId },
                            { "appointmentId", request.AppointmentId?.ToString() ?? "" },
                            { "serviceId", request.ServiceId?.ToString() ?? "" },
                            { "sessionId", "" } // Will be populated after session creation
                        }
                    },
                    Metadata = new Dictionary<string, string>
                    {
                        { "paymentType", paymentType },
                        { "providerId", providerId.ToString() },
                        { "customerId", userId },
                        { "appointmentId", request.AppointmentId?.ToString() ?? "" },
                        { "serviceId", request.ServiceId?.ToString() ?? "" },
                        { "customerNotes", request.CustomerNotes ?? "" }
                    }
                };

                if (!string.IsNullOrEmpty(request.CouponId))
                {
                    var discount = provider.Discounts.FirstOrDefault(x => x.StripeId == request.CouponId);

                    if (discount != null)
                    {
                        sessionOptions.Discounts = new List<SessionDiscountOptions>
                        {
                            new SessionDiscountOptions
                            {
                                Coupon = request.CouponId
                            }
                        };
                    }
                }

                var service = new SessionService();
                Session session = service.Create(sessionOptions);

                return Ok(new
                {
                    CheckoutUrl = session.Url,
                    PriceAmount = price,
                    ApplicationFee = applicationFee,
                    SessionId = session.Id,
                    ConnectedAccount = connectedAccount,
                    PaymentType = paymentType
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

        [HttpPost("refund/booking/{bookingId}")]
        [Authorize]
        public async Task<IActionResult> RefundBooking(Guid bookingId)
        {
            try
            {
                StripeConfiguration.ApiKey = _model.SecretKey;

                // 1. Get the booking
                var booking = await _mediator.Send(new GetBookingByIdQuery(bookingId));
                if (booking == null)
                    return NotFound("Booking not found");

                // Check if the booking can be canceled
                if ((booking.StartTime - DateTime.UtcNow).TotalHours <= 1)
                {
                    return BadRequest("Booking cannot be canceled within 1 hour of the appointment.");
                }

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
                    Status = refund.Status,
                    Type = "booking"
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

        [HttpPost("refund/service-order/{orderId}")]
        [Authorize]
        public async Task<IActionResult> RefundServiceOrder(Guid orderId)
        {
            try
            {
                StripeConfiguration.ApiKey = _model.SecretKey;

                // 1. Get the service order
                var order = await _mediator.Send(new GetServiceOrderByIdQuery(orderId));
                if (order == null)
                    return NotFound("Service order not found");

                // Check if the order can be canceled (within 24 hours of order date or before work starts)
                var hoursSinceOrder = (DateTime.UtcNow - order.OrderDate).TotalHours;
                if (hoursSinceOrder > 24 && order.Status != OrderStatus.Pending)
                {
                    return BadRequest("Service order cannot be canceled after 24 hours or once work has started.");
                }

                // 2. Check authorization (only the customer or provider can refund)
                var currentUserId = User.FindFirstValue(ClaimTypes.NameIdentifier);

                var isCustomer = order.Customer?.UserId == currentUserId;
                var isProvider = order.Provider?.UserId == currentUserId;

                if (!isCustomer && !isProvider)
                    return Forbid("You don't have permission to refund this service order");

                // 3. Check if the order has a payment ID
                if (string.IsNullOrEmpty(order.PaymentIntentId))
                    return BadRequest("No payment information associated with this service order");

                // 4. Check if order is already completed
                if (order.Status == OrderStatus.Completed)
                    return BadRequest("Cannot refund a completed service order");

                // 5. Create refund via Stripe
                var refundService = new RefundService();
                var refundOptions = new RefundCreateOptions
                {
                    PaymentIntent = order.PaymentIntentId,
                    Reason = RefundReasons.RequestedByCustomer
                };

                var refund = await refundService.CreateAsync(refundOptions);

                // 6. Update order status to cancelled and delete
                order.Status = OrderStatus.Cancelled;
                await _mediator.Send(new DeleteServiceOrderCommand(orderId));

                return Ok(new
                {
                    Success = true,
                    RefundId = refund.Id,
                    Amount = refund.Amount,
                    Status = refund.Status,
                    Type = "service-order"
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

        // Keep the original refund endpoint for backward compatibility
        [HttpPost("refund/{bookingId}")]
        [Authorize]
        public async Task<IActionResult> RefundPayment(Guid bookingId)
        {
            return await RefundBooking(bookingId);
        }

        [HttpPost("webhook")]
        public async Task<IActionResult> Webhook()
        {
            _logger.LogInformation("Stripe webhook endpoint entered at {Timestamp}", DateTime.UtcNow);

            var json = await new StreamReader(HttpContext.Request.Body).ReadToEndAsync();

            _logger.LogInformation("Webhook request body received. Length: {Length} characters", json.Length);

            try
            {
                StripeConfiguration.ApiKey = _model.SecretKey;

                var stripeEvent = EventUtility.ConstructEvent(json,
                    Request.Headers["Stripe-Signature"], _webhookSecret);

                _logger.LogInformation("Stripe event constructed successfully. Event type: {EventType}, Event ID: {EventId}",
                    stripeEvent.Type, stripeEvent.Id);

                // Handle the event
                if (stripeEvent.Type == "checkout.session.completed")
                {
                    _logger.LogInformation("Processing checkout.session.completed event");

                    var session = stripeEvent.Data.Object as Session;

                    _logger.LogInformation("Session details - ID: {SessionId}, PaymentStatus: {PaymentStatus}, PaymentIntentId: {PaymentIntentId}",
                        session.Id, session.PaymentStatus, session.PaymentIntentId ?? "NULL");

                    if (session.PaymentStatus == "paid")
                    {
                        _logger.LogInformation("Payment confirmed for session {SessionId}, payment status: {PaymentStatus}",
                            session.Id, session.PaymentStatus);

                        string paymentIntentId = null;

                        // Try multiple approaches to get the PaymentIntentId
                        if (!string.IsNullOrEmpty(session.PaymentIntentId))
                        {
                            paymentIntentId = session.PaymentIntentId;
                            _logger.LogInformation("Found PaymentIntentId directly from session: {PaymentIntentId}", paymentIntentId);
                        }
                        else
                        {
                            _logger.LogInformation("PaymentIntentId is null, trying to retrieve session with expanded PaymentIntent");

                            try
                            {
                                var sessionService = new SessionService();
                                var expandedSession = await sessionService.GetAsync(session.Id, new SessionGetOptions
                                {
                                    Expand = new List<string> { "payment_intent" }
                                });

                                _logger.LogInformation("Expanded session retrieved. PaymentIntent available: {HasPaymentIntent}",
                                    expandedSession?.PaymentIntent != null);

                                if (expandedSession?.PaymentIntent != null)
                                {
                                    paymentIntentId = expandedSession.PaymentIntent.Id;
                                    _logger.LogInformation("Retrieved PaymentIntentId from expanded session: {PaymentIntentId}", paymentIntentId);
                                }
                                else if (!string.IsNullOrEmpty(expandedSession?.PaymentIntentId))
                                {
                                    paymentIntentId = expandedSession.PaymentIntentId;
                                    _logger.LogInformation("Retrieved PaymentIntentId from expanded session property: {PaymentIntentId}", paymentIntentId);
                                }
                                else
                                {
                                    _logger.LogError("Expanded session does not contain PaymentIntent. Session mode: {Mode}, Status: {Status}",
                                        expandedSession?.Mode, expandedSession?.Status);

                                    // Log all available properties for debugging
                                    _logger.LogInformation("Session debug info - Mode: {Mode}, Status: {Status}, Amount Total: {AmountTotal}, Customer: {Customer}",
                                        expandedSession?.Mode,
                                        expandedSession?.Status,
                                        expandedSession?.AmountTotal,
                                        expandedSession?.CustomerId);
                                }
                            }
                            catch (StripeException ex)
                            {
                                _logger.LogError(ex, "Stripe error while expanding session: {Message}", ex.Message);
                            }
                        }

                        // If we still don't have PaymentIntentId, try alternative approaches
                        if (string.IsNullOrEmpty(paymentIntentId))
                        {
                            _logger.LogWarning("Could not retrieve PaymentIntentId through normal methods. Attempting alternative approaches for session {SessionId}", session.Id);

                            // Alternative 1: Use session ID as a temporary identifier and process the booking anyway
                            // This is a fallback approach - you might want to implement a different strategy
                            paymentIntentId = $"session_{session.Id}"; // Temporary fallback

                            _logger.LogWarning("Using session-based fallback PaymentIntentId: {PaymentIntentId}", paymentIntentId);

                            // You could also:
                            // 1. Store this booking with a placeholder and update it later when payment_intent.succeeded fires
                            // 2. Skip processing now and handle it in payment_intent.succeeded webhook
                            // 3. Use the session ID as the payment reference
                        }

                        var paymentType = session.Metadata.GetValueOrDefault("paymentType", "appointment");
                        _logger.LogInformation("Payment type: {PaymentType}", paymentType);

                        if (paymentType == "appointment")
                        {
                            _logger.LogInformation("Processing appointment booking");

                            // Handle appointment booking
                            var providerId = Guid.Parse(session.Metadata["providerId"]);
                            var customerId = session.Metadata["customerId"];
                            var appointmentId = Guid.Parse(session.Metadata["appointmentId"]);

                            _logger.LogInformation("Booking details - Provider: {ProviderId}, Customer: {CustomerId}, Appointment: {AppointmentId}, PaymentIntentId: {PaymentIntentId}",
                                providerId, customerId, appointmentId, paymentIntentId);

                            var bookingCommand = new BookingCommand(providerId, customerId, paymentIntentId, appointmentId);
                            await _mediator.Send(bookingCommand);

                            _logger.LogInformation("Appointment booking completed successfully");
                        }
                        else if (paymentType == "service")
                        {
                            _logger.LogInformation("Processing service order");

                            // Handle service order
                            var serviceId = Guid.Parse(session.Metadata["serviceId"]);
                            var customerId = session.Metadata["customerId"];
                            var customerNotes = session.Metadata.GetValueOrDefault("customerNotes");

                            _logger.LogInformation("Service order details - Service: {ServiceId}, Customer: {CustomerId}, PaymentIntentId: {PaymentIntentId}",
                                serviceId, customerId, paymentIntentId);

                            var orderDto = new CreateServiceOrderDto(serviceId, customerNotes);
                            var orderCommand = new CreateServiceOrderCommand(customerId, orderDto, paymentIntentId);
                            await _mediator.Send(orderCommand);

                            _logger.LogInformation("Service order completed successfully");
                        }
                    }
                    else
                    {
                        _logger.LogWarning("Session payment status is not 'paid'. Status: {PaymentStatus}", session.PaymentStatus);
                    }
                }
                else if (stripeEvent.Type == "payment_intent.succeeded")
                {
                    _logger.LogInformation("Processing payment_intent.succeeded event as backup");

                    var paymentIntent = stripeEvent.Data.Object as PaymentIntent;
                    _logger.LogInformation("PaymentIntent succeeded - ID: {PaymentIntentId}, Status: {Status}",
                        paymentIntent.Id, paymentIntent.Status);

                    // This could be used as a backup to update bookings that were created with session fallback IDs
                    // Implementation depends on your specific requirements
                }
                else
                {
                    _logger.LogInformation("Webhook event type {EventType} not handled", stripeEvent.Type);
                }

                _logger.LogInformation("Webhook processing completed successfully");
                return Ok();
            }
            catch (StripeException e)
            {
                _logger.LogError(e, "Stripe exception occurred while processing webhook: {Message}", e.Message);
                return BadRequest(e.Message);
            }
            catch (Exception e)
            {
                _logger.LogError(e, "Unexpected error occurred while processing webhook: {Message}", e.Message);
                return StatusCode(500, "Internal server error");
            }
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

        [HttpGet("connected-account-link")]
        [Authorize]
        public async Task<IActionResult> GetConnectedAccountLink([FromQuery] string type = "onboarding")
        {
            StripeConfiguration.ApiKey = _model.SecretKey;

            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            var user = (await _gamerRepo.ListAsync()).FirstOrDefault(x => x.UserId == userId);

            if (user == null || string.IsNullOrEmpty(user.StripeAccount))
            {
                return BadRequest("User has no connected Stripe account.");
            }

            try
            {
                var accountLinkOptions = new AccountLinkCreateOptions
                {
                    Account = user.StripeAccount,
                    RefreshUrl = "https://yourfrontend.com/onboarding/refresh",  // TODO: Update to your actual frontend route
                    ReturnUrl = "https://yourfrontend.com/dashboard",             // TODO: Update as needed
                    Type = type == "update" ? "account_update" : "account_onboarding"
                };

                var accountLinkService = new AccountLinkService();
                var link = await accountLinkService.CreateAsync(accountLinkOptions);

                return Ok(new
                {
                    Url = link.Url,
                    Type = accountLinkOptions.Type
                });
            }
            catch (StripeException ex)
            {
                return StatusCode(500, new { Error = ex.Message });
            }
        }

        [HttpGet("is-onboarding-complete")]
        [Authorize]
        public async Task<IActionResult> IsOnboardingComplete()
        {
            StripeConfiguration.ApiKey = _model.SecretKey;

            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            var user = (await _gamerRepo.ListAsync()).FirstOrDefault(x => x.UserId == userId);

            if (user == null || string.IsNullOrEmpty(user.StripeAccount))
            {
                return Ok(new { OnboardingComplete = false });
            }

            try
            {
                var accountService = new AccountService();
                var account = await accountService.GetAsync(user.StripeAccount);

                return Ok(new
                {
                    OnboardingComplete = account.DetailsSubmitted
                });
            }
            catch (StripeException ex)
            {
                return StatusCode(500, new { Error = ex.Message });
            }
        }


        [HttpGet("connected-account-remediation-link")]
        [Authorize]
        public async Task<IActionResult> GetRemediationLink()
        {
            StripeConfiguration.ApiKey = _model.SecretKey;

            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            var user = (await _gamerRepo.ListAsync()).FirstOrDefault(x => x.UserId == userId);

            if (user == null || string.IsNullOrEmpty(user.StripeAccount))
            {
                return BadRequest("User has no connected Stripe account.");
            }

            var accountLinkOptions = new AccountLinkCreateOptions
            {
                Account = user.StripeAccount,
                RefreshUrl = "https://yourfrontend.com/onboarding/refresh",
                ReturnUrl = "https://yourfrontend.com/dashboard",
                Type = "account_onboarding" // This covers onboarding + remediation
            };

            var accountLinkService = new AccountLinkService();
            var link = await accountLinkService.CreateAsync(accountLinkOptions);

            return Ok(new
            {
                Url = link.Url
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

        [HttpGet("validate-coupon-by-name/{couponName}")]
        public async Task<IActionResult> ValidateCouponByName(string couponName)
        {
            try
            {
                // First check our local database for a discount with this name
                var localDiscount = (await _discountRepo.ListAsync()).FirstOrDefault(x => x.Name.ToLower() == couponName.ToLower());
                
                if (localDiscount == null)
                {
                    return Ok(new { Valid = false, Message = "Coupon not found" });
                }

                // Then validate with Stripe using the StripeId
                StripeConfiguration.ApiKey = _model.SecretKey;
                var couponService = new CouponService();
                var coupon = await couponService.GetAsync(localDiscount.StripeId);

                bool isValid = coupon != null && coupon.Valid;

                if (isValid)
                {
                    return Ok(new
                    {
                        Valid = true,
                        CouponId = coupon.Id,
                        Name = coupon.Name,
                        PercentOff = coupon.PercentOff,
                        ExpiresAt = coupon.RedeemBy
                    });
                }
                else
                {
                    return Ok(new { Valid = false, Message = "Coupon is no longer valid" });
                }
            }
            catch (StripeException)
            {
                return Ok(new { Valid = false, Message = "Error validating coupon" });
            }
        }

        [HttpGet("my-coupons")]
        [Authorize]
        public async Task<IActionResult> GetMyCoupons()
        {
            try
            {
                var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);

                if (userId == null)
                {
                    return BadRequest("User not found");
                }

                var user = (await _gamerRepo.ListAsync()).FirstOrDefault(x => x.UserId == userId);

                if (user == null)
                {
                    return BadRequest("User not found");
                }

                // Get user's discounts from local database
                var userDiscounts = (await _discountRepo.ListAsync())
                    .Where(d => d.UserId == user.Id)
                    .ToList();

                if (!userDiscounts.Any())
                {
                    return Ok(new { Coupons = new List<object>() });
                }

                // Validate each coupon with Stripe and prepare response
                StripeConfiguration.ApiKey = _model.SecretKey;
                var couponService = new CouponService();
                var coupons = new List<object>();

                foreach (var discount in userDiscounts)
                {
                    try
                    {
                        var stripeCoupon = await couponService.GetAsync(discount.StripeId);
                        
                        coupons.Add(new
                        {
                            Id = discount.Id,
                            StripeId = discount.StripeId,
                            Name = discount.Name,
                            PercentOff = discount.PercentOff,
                            Duration = discount.Duration,
                            MaxRedemptions = discount.MaxRedemptions,
                            Valid = stripeCoupon?.Valid ?? false,
                            ExpiresAt = stripeCoupon?.RedeemBy,
                            TimesRedeemed = stripeCoupon?.TimesRedeemed ?? 0,
                            Created = stripeCoupon?.Created
                        });
                    }
                    catch (StripeException)
                    {
                        // If Stripe coupon doesn't exist or there's an error, still include it but mark as invalid
                        coupons.Add(new
                        {
                            Id = discount.Id,
                            StripeId = discount.StripeId,
                            Name = discount.Name,
                            PercentOff = discount.PercentOff,
                            Duration = discount.Duration,
                            MaxRedemptions = discount.MaxRedemptions,
                            Valid = false,
                            ExpiresAt = (DateTime?)null,
                            TimesRedeemed = 0,
                            Created = (DateTime?)null,
                            Error = "Coupon not found in Stripe"
                        });
                    }
                }

                return Ok(new
                {
                    Coupons = coupons,
                    TotalCount = coupons.Count
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { Error = $"Error retrieving coupons: {ex.Message}" });
            }
        }
    }
}
