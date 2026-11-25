namespace Core.Data
{
    /// <summary>
    ///     Sample custom type that demonstrates non-standard JSON serialization.
    ///     Used with <see cref="KinoaCustomJsonConverterSample"/> to serialize boolean as 0/1 integer.
    /// </summary>
    public class CustomBool
    {
        public bool Value { get; set; }
    }
}
