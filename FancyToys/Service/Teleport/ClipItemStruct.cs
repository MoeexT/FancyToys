using MemoryPack;


namespace FancyToys.Service.Teleport;

[MemoryPackable]
// ReSharper disable once PartialTypeWithSinglePart
public partial struct ClipItemStruct {
    [MemoryPackInclude]
    public bool Pinned;
    [MemoryPackInclude]
    public ClipType ContentType;
    [MemoryPackInclude]
    public string Uri;
    [MemoryPackInclude]
    public string Text;
    [MemoryPackInclude]
    public string[] Paths;
    [MemoryPackInclude]
    public byte[] ImageBytes;

    public override string ToString() => $"{{ " +
        $"Pinned: {Pinned}, " +
        $"ContentType: {ContentType}, " +
        $"Uri: {(Uri ?? "null")}, " +
        $"Text: {(Text ?? "null")}, " +
        $"Paths: [{string.Join(", ", Paths ?? new[] { "null" })}], " +
        $"ImageBytes: {ImageBytes?.Length ?? -1}, " +
        $" }}";
}
