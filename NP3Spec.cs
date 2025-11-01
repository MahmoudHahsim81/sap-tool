namespace _3D_SAP
{
    public class NP3Spec
    {
        public string Name { get; set; }

        public int H1, TFW1, TFT1, TW1, BFW1, BFT1;
        public int H2, TFW2, TFT2, TW2, BFW2, BFT2;
        public int H3, TFW3, TFT3, TW3, BFW3, BFT3;

        public double R1, R2, R3; // مجموعها = 1 بعد التطبيع
    }
}
