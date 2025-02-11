using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Drawing;

namespace AutomataVideoGenerator
{
    public interface iAutomaton
    {
        public Bitmap getImage();
        public void update();
        public void run(int cycles, string savePath);
    }
}
