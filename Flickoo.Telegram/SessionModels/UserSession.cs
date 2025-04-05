using Flickoo.Telegram.enums;

namespace Flickoo.Telegram.SessionModels
{
    public class UserSession
    {
        public UserSessionState State { get; set; } = UserSessionState.Idle;
        public string UserName { get; set; } = string.Empty;
        public string LocationName {  get; set; } = string.Empty;
        public string? Action { get; set; } = null;
    }
}
