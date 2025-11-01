using System;

public enum PreviewKind
{
    None,
    ISection,
    RHS,
    Hunch3Segments   // جديد
}

public class PreviewModel
{
    public PreviewKind Kind { get; set; }

    // I-Section
    public int H { get; set; }
    public int BTop { get; set; }
    public int TfTop { get; set; }
    public int Tw { get; set; }
    public int BBot { get; set; }
    public int TfBot { get; set; }

    // RHS / SHS
    public double Depth { get; set; }
    public double Width { get; set; }
    public double T { get; set; }

    // Hunch 3-seg
    public Seg[] Segs { get; set; } = Array.Empty<Seg>();
    public class Seg
    {
        public int H { get; set; }
        public int BTop { get; set; }
        public int TfTop { get; set; }
        public int Tw { get; set; }
        public int BBot { get; set; }
        public int TfBot { get; set; }
        public double LRatio { get; set; } // نسبة طول السيجمنت
    }
}
