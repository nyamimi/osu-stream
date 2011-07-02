using System;
using osum.Helpers;
using osum.UI;

namespace osum.Online
{
    public static class OnlineHelper
    {
        /// <summary>
        /// Gets a value indicating whether online functionality is available.
        /// </summary>
        public static bool Available {
            get
            {
                return Initialize() && onlineServices.IsAuthenticated;
            }
        }

        public static bool Initialize()
        {
            if (onlineServices == null)
            {
#if iOS
                onlineServices = new OnlineServicesIOS();
#endif
            }

            if (onlineServices != null)
                onlineServices.Authenticate(authFinished);
            else
                authFinished();

            return onlineServices != null;
        }

        static void authFinished()
        {
            if (onlineServices != null && onlineServices.IsAuthenticated)
                //we succeeded, so reset the warning
                GameBase.Config.SetValue<bool>("GamecentreFailureAnnounced", false);
            else
            {
                if (!GameBase.Config.GetValue<bool>("GamecentreFailureAnnounced", false))
                {
                    Notification n = new Notification(osum.Resources.General.GameCentreInactive, osum.Resources.General.GameCentreInactiveExplanation, NotificationStyle.Okay);
                    GameBase.Notify(n);
                    GameBase.Config.SetValue<bool>("GamecentreFailureAnnounced", true);
                }
            }

        }

        public static bool ShowRanking(string id, VoidDelegate finished = null)
        {
            if (!Initialize()) return false;

            onlineServices.ShowLeaderboard(id, finished);
            return true;
        }

        public static bool SubmitScore(string id, int score, VoidDelegate finished = null)
        {
            if (!Initialize()) return false;

            onlineServices.SubmitScore(id, score, finished);
            return true;
        }

        static IOnlineServices onlineServices;
    }
}

