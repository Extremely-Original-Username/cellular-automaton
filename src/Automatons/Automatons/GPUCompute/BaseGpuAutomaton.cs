using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Linq;
using System.Numerics;
using System.Text;
using System.Threading.Tasks;
using ComputeSharp;

namespace AutomataVideoGenerator.Automatons.GPUCompute
{
    public abstract partial class BaseGpuAutomaton : iAutomaton, IDisposable
    {
        protected ReadWriteTexture2D<int> texture;
        private int scale;

        public BaseGpuAutomaton(int width, int height, int scale)
        {
            this.scale = scale;

            texture = GraphicsDevice.GetDefault().AllocateReadWriteTexture2D<int>(width, height);
        }

        public Bitmap getImage()
        {
            int buffer = 1;

            int[,] textureArray = texture.ToArray();
            Bitmap bitmap = new Bitmap(texture.Width + buffer * 2, texture.Height + buffer * 2);

            for (int y = 0; y < bitmap.Height; y++)
            {
                for (int x = 0; x < bitmap.Width; x++)
                {
                    if (
                        y - buffer >= 0 && y - buffer < textureArray.GetLength(0) &&
                        x - buffer >= 0 && x - buffer < textureArray.GetLength(1)
                        )
                    {
                        var pixel = textureArray[y - buffer, x - buffer]; //This must be flipped as the texture stores the width/height the wrong way around
                        if (pixel == 1)
                        {
                            bitmap.SetPixel(x, y, Color.White);
                        }
                        else
                        {
                            bitmap.SetPixel(x, y, Color.Black);
                        }
                    }
                    else
                    {
                        bitmap.SetPixel(x, y, Color.Gray);
                    }
                }
            }

            return resizeImage(bitmap, bitmap.Width * scale, bitmap.Height * scale);
        }

        private static Bitmap resizeImage(Bitmap image, int newWidth, int newHeight)
        {
            Bitmap result = new Bitmap(newWidth, newHeight);
            using (Graphics g = Graphics.FromImage(result))
            {
                g.InterpolationMode = InterpolationMode.NearestNeighbor; // Disable anti-aliasing
                g.DrawImage(image, 0, 0, newWidth, newHeight);
            }
            return result;
        }

        public abstract void update();

        public void run(int cycles, string savePath, int scale)
        {
            Directory.CreateDirectory(savePath);

            for (int i = 0; i < cycles; i++)
            {
                Console.SetCursorPosition(0, 0);
                Console.WriteLine("Processing: " + i + "                  ");
                getImage().Save(savePath + "/" + i + ".bmp", System.Drawing.Imaging.ImageFormat.Bmp);
                update();
            }
        }
        public void run(int cycles, string savePath)
        {
            run(cycles, savePath, 1);
        }

        public void setRandom()
        {
            int width = texture.Width;
            int height = texture.Height;

            int[,] startingMap = new int[height, width]; //This is also flipped due to issues with the texture field's dimensions being flipped.

            for (int y = 0; y < height; y++)
            {
                for (int x = 0; x < width; x++)
                {
                    startingMap[y, x] = new Random().Next() % 2;
                }
            }

            var initial = GraphicsDevice.GetDefault().AllocateReadWriteTexture2D<int>(startingMap);

            GraphicsDevice.GetDefault().For(texture.Width, texture.Height, new
                Set(texture, initial));

            initial.Dispose();
        }

        [AutoConstructor]
        public readonly partial struct Set : IComputeShader
        {
            public readonly ReadWriteTexture2D<int> buffer;
            public readonly ReadWriteTexture2D<int> source;

            public void Execute()
            {
                buffer[ThreadIds.XY] = source[ThreadIds.XY];
            }
        }

        public void Dispose()
        {
            this.texture.Dispose();
        }
    }
}
