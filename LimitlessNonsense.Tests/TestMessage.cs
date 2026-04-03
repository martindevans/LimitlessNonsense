namespace LimitlessNonsense.Tests
{
    public sealed record TestMessage(Guid ID, MessageRole Role, Importance Importance)
        : IContextMessage
    {
        public string Prefix { get; set; } = "";
        public string Content { get; set; } = "";
        public string Suffix { get; set; } = "";

        private readonly Dictionary<Type, object?> _metadata = [];

        public void SetMetadata<TMetadata>(TMetadata metadata)
            where TMetadata : class, IMessageMetadata
        {
            _metadata[typeof(TMetadata)] = metadata;
        }

        public TMetadata? TryGetMetadata<TMetadata>()
            where TMetadata : class, IMessageMetadata
        {
            return (TMetadata?)_metadata.GetValueOrDefault(typeof(TMetadata), null);
        }
    }
}
