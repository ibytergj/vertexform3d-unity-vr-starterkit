// Read the already-loaded baseline capture, without loading assets or running the game.
using var frame = UnityEditorInternal.ProfilerDriver.GetRawFrameDataView(0, 0);
if (!frame.valid) throw new InvalidOperationException("Load the baseline profiler file first.");
var output = new System.Collections.Generic.List<object>();
for (int i = 0; i < frame.sampleCount; i++)
{
    string name = frame.GetSampleName(i);
    if (frame.GetSampleTimeMs(i) < 100 || !(name.Contains("Loading.ReadObject") || name == "File.Open" || name.Contains("DynamicCharacterAvatar.Start"))) continue;
    var metadata = new System.Collections.Generic.List<object>();
    var info = frame.GetMarkerMetadataInfo(frame.GetSampleMarkerId(i));
    for (int j = 0; j < frame.GetSampleMetadataCount(i); j++)
        metadata.Add(new { description = info[j], value = frame.GetSampleMetadataAsString(i, j) });
    output.Add(new { sample=i, name, milliseconds=frame.GetSampleTimeMs(i), metadata });
    if (output.Count >= 35) break;
}
return output;
