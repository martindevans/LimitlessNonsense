//using System.Text.Json;

//namespace LimitlessNonsense
//{
//    /// <summary>
//    /// Core context for interaction with LLM
//    /// </summary>
//    public class ConversationalContext<TMessage>
//        where TMessage : class, IMessage
//    {
//        public string Model { get; }

//        public ConversationalContext(string model)
//        {
//            Model = model;
//        }

//        #region save/load
//        /// <summary>
//        /// Save the messages to a JSON string
//        /// </summary>
//        /// <returns></returns>
//        public string Save()
//        {
//            var json = JsonSerializer.Serialize(_messages);
//            return json;
//        }

//        /// <summary>
//        /// Load messages from the JSON string
//        /// </summary>
//        /// <param name="json"></param>
//        /// <param name="overwriteSystemPrompt"></param>
//        public void Load(string json, bool overwriteSystemPrompt = false)
//        {
//            // Get the system prompt
//            var sys = _messages.Count > 0 ? _messages[0] : null;

//            // Remove all messages
//            _messages.Clear();

//            // Load messages
//            var messages = JsonSerializer.Deserialize<List<ChatMessage>>(json) ?? [];
//            _messages.AddRange(messages);

//            // Restore system prompt
//            if (sys != null && !overwriteSystemPrompt)
//                ReplaceSystemPrompt(sys.GetMessageContent());
//        }
//        #endregion

//        private class Turn
//        {
//            /// <summary>
//            /// Generic metadata attached to this turn
//            /// </summary>
//            private readonly Dictionary<Type, object?> _metadata = new();

//            /// <summary>
//            /// The actual message
//            /// </summary>
//            public TMessage Message { get; }

//            public MessageRole Role => Message.Role;

//            public Turn(TMessage message)
//            {
//                Message = message;
//            }

//            /// <summary>
//            /// Try to get metadata attached to this turn
//            /// </summary>
//            /// <typeparam name="T"></typeparam>
//            /// <returns></returns>
//            public T? GetMetadata<T>(T? @default = default)
//            {
//                if (_metadata.TryGetValue(typeof(T), out var value))
//                    return (T?)value;
//                return @default;
//            }

//            /// <summary>
//            /// Add new metadata, throws if the metadata is already present
//            /// </summary>
//            /// <typeparam name="T"></typeparam>
//            /// <param name="data"></param>
//            public void AddMetadata<T>(T data)
//            {
//                if (_metadata.ContainsKey(typeof(T)))
//                    throw new InvalidOperationException($"Turn already has '{typeof(T)}' metadata attached");
//            }

//            /// <summary>
//            /// Set metadata, overwrites if the metadata is already present
//            /// </summary>
//            /// <typeparam name="T"></typeparam>
//            /// <param name="data"></param>
//            public void SetMetadata<T>(T data)
//            {
//                _metadata[typeof(T)] = data;
//            }
//        }


//    }


//}