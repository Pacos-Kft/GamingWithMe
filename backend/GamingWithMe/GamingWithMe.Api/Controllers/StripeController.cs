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

        public StripeController(IOptions<StripeModel> model, TokenService token, CustomerService customer, ChargeService charge, ProductService product)
        {
            _model = model.Value;
            _token = token;
            _customerService = customer;
            _charge = charge;
            _product = product;
        }

        [HttpPost("pay")]
        public IActionResult Pay([FromBody] string PriceId)
        {
            StripeConfiguration.ApiKey = _model.SecretKey;

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
                CancelUrl = "http://localhost:5173"
            };

            options.Customer = "cus_SaCXeZDWJH5t4L";

            var service = new SessionService();

            Session session = service.Create(options);

            return Ok(session.Url);
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
