using System.Runtime.Serialization;

namespace Riskified.SDK.Model
{
    /// <summary>
    /// Riskified fraud decision status values
    /// Returned in OrderNotification.Status field
    /// </summary>
    public enum NotificationStatus
    {
        /// <summary>
        /// Order approved - safe to fulfill
        /// </summary>
        [EnumMember(Value = "approved")]
        Approved,

        /// <summary>
        /// Order declined - do not fulfill, high fraud risk
        /// </summary>
        [EnumMember(Value = "declined")]
        Declined,

        /// <summary>
        /// Order submitted and pending review
        /// </summary>
        [EnumMember(Value = "submitted")]
        Submitted,

        /// <summary>
        /// Order cancelled
        /// </summary>
        [EnumMember(Value = "cancelled")]
        Cancelled,

        /// <summary>
        /// Order on hold for manual review
        /// </summary>
        [EnumMember(Value = "on_hold")]
        OnHold
    }

    /// <summary>
    /// Extension methods for NotificationStatus
    /// </summary>
    public static class NotificationStatusExtensions
    {
        /// <summary>
        /// Checks if status indicates order is approved and safe to fulfill
        /// </summary>
        public static bool IsApproved(this string status)
        {
            return status?.Equals("approved", System.StringComparison.OrdinalIgnoreCase) == true;
        }

        /// <summary>
        /// Checks if status indicates order is declined (fraud risk)
        /// </summary>
        public static bool IsDeclined(this string status)
        {
            return status?.Equals("declined", System.StringComparison.OrdinalIgnoreCase) == true;
        }

        /// <summary>
        /// Parses string status to enum (if possible)
        /// </summary>
        public static NotificationStatus? ToEnum(this string status)
        {
            return status?.ToLowerInvariant() switch
            {
                "approved" => NotificationStatus.Approved,
                "declined" => NotificationStatus.Declined,
                "submitted" => NotificationStatus.Submitted,
                "cancelled" => NotificationStatus.Cancelled,
                "on_hold" => NotificationStatus.OnHold,
                _ => null
            };
        }
    }
}
