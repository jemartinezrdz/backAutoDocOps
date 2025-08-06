using AutoDocOps.Application.Common.Interfaces;
using AutoDocOps.WebAPI.Models;
using AutoMapper;
using Microsoft.AspNetCore.Mvc;

namespace AutoDocOps.WebAPI.Controllers;

[ApiController]
[Route("api/[controller]")]
public class TestController : ControllerBase
{
    private readonly ICacheService _cacheService;
    private readonly ILlmClient _llmClient;
    private readonly IBillingService _billingService;
    private readonly IMapper _mapper;
    private readonly ILogger<TestController> _logger;
    private readonly IConfiguration _configuration;

    public TestController(ICacheService cacheService, ILlmClient llmClient, IBillingService billingService, IMapper mapper, ILogger<TestController> logger, IConfiguration configuration)
    {
        _cacheService = cacheService;
        _llmClient = llmClient;
        _billingService = billingService;
        _mapper = mapper;
        _logger = logger;
        _configuration = configuration;
    }

    [HttpGet("cache/{key}")]
    public async Task<IActionResult> GetFromCache(string key)
    {
        try
        {
            var value = await _cacheService.GetAsync<string>(key);
            
            if (value == null)
            {
                var newValue = $"Generated value for {key} at {DateTime.UtcNow:yyyy-MM-dd HH:mm:ss} UTC";
                await _cacheService.SetAsync(key, newValue, TimeSpan.FromMinutes(5));
                
                _logger.LogInformation("Cache MISS for key: {Key}. Generated new value.", key);
                
                return Ok(new 
                { 
                    key = key,
                    value = newValue,
                    source = "generated",
                    timestamp = DateTime.UtcNow,
                    ttl_minutes = 5
                });
            }
            
            _logger.LogInformation("Cache HIT for key: {Key}", key);
            
            return Ok(new 
            { 
                key = key,
                value = value,
                source = "cache",
                timestamp = DateTime.UtcNow
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error testing cache for key: {Key}", key);
            return StatusCode(500, new { error = ex.Message });
        }
    }

    [HttpDelete("cache/{key}")]
    public async Task<IActionResult> RemoveFromCache(string key)
    {
        try
        {
            await _cacheService.RemoveAsync(key);
            _logger.LogInformation("Removed key from cache: {Key}", key);
            
            return Ok(new { message = $"Key '{key}' removed from cache" });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error removing key from cache: {Key}", key);
            return StatusCode(500, new { error = ex.Message });
        }
    }

    [HttpGet("health")]
    public IActionResult Health()
    {
        return Ok(new 
        { 
            status = "healthy",
            timestamp = DateTime.UtcNow,
            message = "Test controller is working",
            environment = Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT") ?? "Unknown",
            version = "1.0.0"
        });
    }

    [HttpGet("system-info")]
    [ProducesResponseType(typeof(object), StatusCodes.Status200OK)]
    public IActionResult SystemInfo()
    {
        var systemInfo = new
        {
            timestamp = DateTime.UtcNow,
            environment = Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT") ?? "Unknown",
            machine_name = Environment.MachineName,
            os_version = Environment.OSVersion.ToString(),
            dotnet_version = Environment.Version.ToString(),
            working_set = GC.GetTotalMemory(false),
            services = new
            {
                cache_configured = _cacheService != null,
                llm_configured = _llmClient != null,
                billing_configured = _billingService != null,
                mapper_configured = _mapper != null
            }
        };

        return Ok(systemInfo);
    }

    [HttpPost("chat")]
    public async Task<IActionResult> TestChat([FromBody] string query)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(query))
            {
                query = "Hola, ¿cómo estás? ¿Puedes ayudarme con documentación técnica?";
            }

            var response = await _llmClient.ChatAsync(query);
            
            _logger.LogInformation("Chat test completed for query: {Query}", query);
            
            return Ok(new 
            { 
                query = query,
                response = response,
                timestamp = DateTime.UtcNow,
                llm_type = _llmClient.GetType().Name,
                status = "success"
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error testing chat for query: {Query}", query);
            return StatusCode(500, new { 
                error = ex.Message, 
                query = query,
                timestamp = DateTime.UtcNow,
                status = "error"
            });
        }
    }

    [HttpPost("billing/checkout")]
    public async Task<IActionResult> TestCreateCheckout([FromBody] TestCheckoutRequest request)
    {
        try
        {
            if (request?.OrganizationId == Guid.Empty)
            {
                request = new TestCheckoutRequest 
                { 
                    OrganizationId = Guid.NewGuid(),
                    PlanId = "price_starter_default",
                    SuccessUrl = _configuration["Billing:DefaultSuccessUrl"] ?? "http://localhost:8080/success",
                    CancelUrl = _configuration["Billing:DefaultCancelUrl"] ?? "http://localhost:8080/cancel"
                };
            }

            var sessionUrl = await _billingService.CreateCheckoutSessionAsync(
                request!.OrganizationId, 
                request.PlanId, 
                request.SuccessUrl, 
                request.CancelUrl);
            
            _logger.LogInformation("Billing checkout test completed for organization: {OrgId}, plan: {Plan}", request.OrganizationId, request.PlanId);
            
            return Ok(new 
            { 
                organization_id = request.OrganizationId,
                plan_id = request.PlanId,
                checkout_session_url = sessionUrl,
                timestamp = DateTime.UtcNow,
                billing_service = "Stripe (testing mode)"
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error testing billing checkout for organization: {OrgId}", request?.OrganizationId);
            return StatusCode(500, new { error = ex.Message });
        }
    }

    [HttpGet("mapper")]
    public IActionResult TestAutoMapper()
    {
        try
        {
            // Crear un objeto de prueba para mapear
            var testProject = new TestProject
            {
                Id = Guid.NewGuid(),
                Name = "Test Project",
                Description = "This is a test project for AutoMapper",
                CreatedAt = DateTime.UtcNow
            };

            // Usar AutoMapper para mapear a DTO
            var projectDto = _mapper.Map<TestProjectDto>(testProject);
            
            _logger.LogInformation("AutoMapper test completed for project: {ProjectId}", testProject.Id);
            
            return Ok(new 
            { 
                original = testProject,
                mapped = projectDto,
                timestamp = DateTime.UtcNow,
                mapper_service = "AutoMapper (working)"
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error testing AutoMapper");
            return StatusCode(500, new { error = ex.Message });
        }
    }

    [HttpPost("session")]
    public IActionResult TestSession([FromBody] TestSessionData data)
    {
        try
        {
            // Probar sesiones distribuidas
            var sessionKey = "test_session_data";
            
            if (data != null)
            {
                // Guardar en sesión
                HttpContext.Session.SetString(sessionKey, System.Text.Json.JsonSerializer.Serialize(data));
                _logger.LogInformation("Session data stored for key: {Key}", sessionKey);
                
                return Ok(new 
                { 
                    action = "stored",
                    data = data,
                    session_id = HttpContext.Session.Id,
                    timestamp = DateTime.UtcNow
                });
            }
            else
            {
                // Leer de sesión
                var sessionData = HttpContext.Session.GetString(sessionKey);
                _logger.LogInformation("Session data retrieved for key: {Key}", sessionKey);
                
                return Ok(new 
                { 
                    action = "retrieved",
                    data = sessionData != null ? System.Text.Json.JsonSerializer.Deserialize<TestSessionData>(sessionData) : null,
                    session_id = HttpContext.Session.Id,
                    timestamp = DateTime.UtcNow
                });
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error testing session");
            return StatusCode(500, new { error = ex.Message });
        }
    }
}

