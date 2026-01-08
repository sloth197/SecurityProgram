//특정 로그만 나타남 4개의 이벤트로그를제외한 로그는 숨겨짐
namespace SecurityProgram.Core.Monitoring
{
    public enum EventSecurity
    {
        Information,
        Warning,
        Error,
        FailureAudit
    }
}