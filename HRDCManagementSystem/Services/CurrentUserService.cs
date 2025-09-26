namespace HRDCManagementSystem.Services
{
    public class CurrentUserService : ICurrentUserService
    {
        private readonly IHttpContextAccessor _httpContextAccessor;

        public CurrentUserService(IHttpContextAccessor httpContextAccessor)
        {
            _httpContextAccessor = httpContextAccessor;
        }

        public int? GetCurrentUserId()
        {
            // Try to get from session first
            var userIdFromSession = _httpContextAccessor.HttpContext?.Session.GetInt32("UserSysID");
            if (userIdFromSession.HasValue)
                return userIdFromSession.Value;

            // Fallback to claims
            var userIdClaim = _httpContextAccessor.HttpContext?.User?.FindFirst("UserSysID");
            if (userIdClaim != null && int.TryParse(userIdClaim.Value, out int userId))
                return userId;

            return null;
        }
    }
}