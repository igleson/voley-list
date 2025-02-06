using Microsoft.AspNetCore.Mvc;

namespace api.Controllers;

[ApiController]
[Route("/meta/healthcheck")]
public class MetaController: ControllerBase
{

    [HttpGet]
    public string ListAll()
    {
        return "ok";
    }
}