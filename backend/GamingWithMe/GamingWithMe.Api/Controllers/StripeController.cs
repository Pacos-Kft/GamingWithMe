using GamingWithMe.Domain.Entities;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using Microsoft.Identity.Client;
using Stripe;
using Stripe.Checkout;

namespace GamingWithMe.Api.Controllers
{
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

        public StripeController(IOptions<StripeModel> model, TokenService token, CustomerService customer, ChargeService charge, ProductService product)
        {
            _model = model.Value;
            _token = token;
            _customerService = customer;
            _charge = charge;
            _product = product;
            _priceService = new PriceService();
        }

        [HttpPost("pay")]
        public IActionResult Pay([FromBody] string PriceId)
        {
            try
            {
                StripeConfiguration.ApiKey = _model.SecretKey;

                var connectedAccount = "acct_1Rf3tXCHximbn0NA";

                // Retrieve the actual price information
                var price = _priceService.Get(PriceId);
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
                            Price = PriceId,
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
                    }
                };

                options.Customer = "cus_SaCXeZDWJH5t4L";

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
            catch (Exception ex)
            {
                return StatusCode(500, $"Error processing payment: {ex.Message}");
            }
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
    }
}
