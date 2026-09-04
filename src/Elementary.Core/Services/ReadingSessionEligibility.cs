namespace Elementary.Core.Services
{
    public static class ReadingSessionEligibility
    {
        public static bool ShouldCount(
            bool isReaderLoaded,
            bool isReaderPageActive,
            bool isWindowVisible,
            bool isWindowActive,
            bool isReaderObscured,
            bool isApplicationSuspended)
        {
            return isReaderLoaded
                   && isReaderPageActive
                   && isWindowVisible
                   && isWindowActive
                   && !isReaderObscured
                   && !isApplicationSuspended;
        }
    }
}
