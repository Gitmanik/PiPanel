using System.Threading.Tasks;

namespace PiPanelCore.LCD
{
    public enum LCDRotation
    {
        VERTICAL, VERTICAL_REV, HORIZONTAL, HORIZONTAL_REV
    }

    public class RGB
    {
        public int R = 0, G = 0, B = 0;

        public RGB()
        {
        }

        public RGB(int R, int G, int B)
        {
            this.R = R;
            this.G = G;
            this.B = B;
        }
        public override string ToString() => $"[RGB: {R} {G} {B}]";
    }


    public interface ILCD
    {
        public int Width { get; }
        public int Height { get; }

        public Task Reset();
        public Task Setup();
        public void Pixel(int x, int y, RGB color);
        public void Rect(int x1, int y1, int x2, int y2, RGB color);
    }
}
