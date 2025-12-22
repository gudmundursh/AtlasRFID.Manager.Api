namespace AtlasRFID.Manager.Api.Services
{
    public interface ICorrelationIdProvider
    {
        string Get();
    }

    public class CorrelationIdProvider : ICorrelationIdProvider
    {
        private readonly IHttpContextAccessor _ctx;

        public CorrelationIdProvider(IHttpContextAccessor ctx)
        {
            _ctx = ctx;
        }

        public string Get()
        {
            // use ASP.NET trace id
            return _ctx.HttpContext?.TraceIdentifier;
        }
    }
}
