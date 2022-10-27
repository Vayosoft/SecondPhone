namespace EmulatorRC.API.Model
{
    public class Emulator : IEquatable<Emulator?>
    {
        public string Id { get; }

        public Emulator(string id)
        {
            Id = id;
        }

        public long LastAccessTime { get; set; }
        public int Orientation { get; set; }

        public override bool Equals(object? obj)
        {
            return Equals(obj as Emulator);
        }

        public bool Equals(Emulator? other)
        {
            return other is not null &&
                   Id == other.Id;
        }

        public override int GetHashCode()
        {
            return HashCode.Combine(Id);
        }

        public static bool operator ==(Emulator? left, Emulator? right)
        {
            return EqualityComparer<Emulator>.Default.Equals(left, right);
        }

        public static bool operator !=(Emulator? left, Emulator? right)
        {
            return !(left == right);
        }
    }
}

