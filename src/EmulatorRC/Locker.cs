namespace EmulatorRC
{
    public sealed class Locker
    {
        private readonly string _name;

        private static readonly Dictionary<string, RefCounted<object>> Locks = new();

        public Locker(string name)
        {
            _name = name;
        }

        public static Locker GetLockerByName(string name)
        {
            return new Locker(name);
        }

        private static object GetOrCreate(string name)
        {
            RefCounted<object> item;
            lock (Locks)
            {
                if (Locks.TryGetValue(name, out item!))
                {
                    ++item.RefCount;
                }
                else
                {
                    item = new RefCounted<object>(new object());
                    Locks[name] = item;
                }
            }
            return item.Value;
        }

        public IDisposable Lock()
        {
            Monitor.Enter(GetOrCreate(_name));
            return new LockExit(_name);
        }

        private readonly struct LockExit : IDisposable
        {
            private readonly string _name;

            public LockExit(string name)
            {
                _name = name;
            }

            public void Dispose()
            {
                RefCounted<object> item;
                lock (Locks)
                {
                    item = Locks[_name];
                    --item.RefCount;
                    if (item.RefCount == 0)
                    {
                        Locks.Remove(_name);
                    }
                }

                if (Monitor.IsEntered(item.Value))
                {
                    Monitor.Exit(item.Value);
                }
            }
        }

        private sealed class RefCounted<T>
        {
            public RefCounted(T value)
            {
                RefCount = 1;
                Value = value;
            }

            public int RefCount { get; set; }
            public T Value { get; }
        }
    }
}
