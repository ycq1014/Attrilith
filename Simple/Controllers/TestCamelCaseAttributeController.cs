using Attrilith.Controller;
using Microsoft.AspNetCore.Mvc;
using Simple.Services;

namespace Simple.Controllers;

[CamelCaseRoute]
[ApiController]
[Route("api/[controller]")]
public class TestCamelCaseAttributeController : ControllerBase
{
    private readonly TestServiceAttributeService _serviceAttributeService;

    public TestCamelCaseAttributeController(TestServiceAttributeService serviceAttributeService)
    {
        _serviceAttributeService = serviceAttributeService;
    }

    [HttpGet("testCamelCaseAttribute")]
    public IActionResult Demo()
    {
        return Ok(new
        {
            print = "testCamelCaseAttribute",
        });
    }
}