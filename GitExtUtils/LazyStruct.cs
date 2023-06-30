namespace GitExtUtils
{
    public class LazyStruct<T> where T : struct
    {
        private T _value = default;
        private Func<T> _valueFactory;

        public LazyStruct(Func<T> valueFactory)
        {
            _valueFactory = valueFactory;
        }

        /// <summary>
        /// Returns whether a value has been created for this instance.
        /// </summary>
        public bool IsValueCreated { get; private set; } = false;

        /// <summary>
        /// Gets the lazily initialized value.
        /// </summary>
        public T Value
        {
            get
            {
                if (!IsValueCreated)
                {
                    _value = _valueFactory();
                    IsValueCreated = true;
                }

                return _value;
            }
        }
    }
}
