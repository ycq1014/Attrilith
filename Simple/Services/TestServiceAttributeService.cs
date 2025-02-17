using Attrilith.Service;

namespace Simple.Services;

[Service]
public class TestServiceAttributeService
{
    string PrintAutoRegister()
    {
        return "PrintAutoRegister -- TestServerAttribute";
    }
}