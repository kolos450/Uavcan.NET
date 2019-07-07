namespace CanardSharp.Dsdl.DataTypes
{
    public abstract class DsdlType
    {
        public abstract ulong? GetDataTypeSignature();

        public abstract string GetNormalizedMemberDefinition();

        public override string ToString() =>
            GetNormalizedMemberDefinition();

        public abstract int MaxBitlen { get; }
        public abstract int MinBitlen { get; }
    }
}
