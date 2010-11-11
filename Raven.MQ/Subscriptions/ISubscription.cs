namespace RavenMQ.Subscriptions
{
    public interface ISubscription
    {
        bool IsMatch(SubscriptionFilterInfo subscriptionFilterInfo);
        void NotifyChanges();
    }
}