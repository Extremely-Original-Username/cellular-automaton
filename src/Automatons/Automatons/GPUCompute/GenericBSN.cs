using ComputeSharp;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TerraFX.Interop.Windows;

namespace AutomataVideoGenerator.Automatons.GPUCompute
{
    //Generic Binary Square Neighborhood
    public partial class GenericBSN : BaseGpuAutomaton
    {
        private int live = 1;
        private int dead = 0;

        private int neighborhoodSize;

        private bool largerThanLife = false;
        private int[] bornAt, surviveAt;
        private int bornMin, bornMax, surviveMin, surviveMax;

        public enum defaults
        {
            GameOfLife,
            Maze,
            Mazecetric,
            DayAndNight,
            Bugs,
        }

        private static Dictionary<defaults, SBNRuleSet> defaultsRuleSets = new Dictionary<defaults, SBNRuleSet>()
        {
            { defaults.GameOfLife, new SBNRuleSet(1, "B3/S23") },
            { defaults.Maze, new SBNRuleSet(1, "B3/S12345") },
            { defaults.Mazecetric, new SBNRuleSet(1, "B3/S1234") },
            { defaults.DayAndNight, new SBNRuleSet(1, "B3678/S34678") },
            { defaults.Bugs, new SBNRuleSet(5, "33,45,33,58") },
        };


        public GenericBSN(int width, int height, int scale, int neighborhoodSize, string ruleString) : base(width, height, scale)
        {
            this.neighborhoodSize = neighborhoodSize;

            parseRuleString(ruleString);

            setRandom();
        }

        public GenericBSN(int width, int height, int scale, defaults ruleSet) : base(width, height, scale)
        {
            var v = defaultsRuleSets[ruleSet];

            this.neighborhoodSize = v.neighborhoodSize;

            parseRuleString(v.ruleString);

            setRandom();
        }
        
        private void parseRuleString(string rules)
        {
            if (rules.Contains(","))
            {
                largerThanLife = true;
            }

            if (!largerThanLife)
            {
                string[] substrings = rules.Split("/");
                if (substrings.Length != 2)
                {
                    throw new Exception("Invalid rule string - two sections not found");
                }

                bornAt = new int[substrings[0].Length - 1];
                surviveAt = new int[substrings[1].Length - 1];

                for (int i = 0; i < 2; i++)
                {
                    if (!substrings[i].Contains(i == 0 ? "B" : "S"))
                    {
                        throw new Exception("Invalid rule string - " + (i == 0 ? "born" : "survive") + " not found.");
                    }
                    else
                    {
                        for (int j = 1; j < substrings[i].Length; j++)
                        {
                            if (i == 0)
                            {
                                bornAt[j - 1] = int.Parse(substrings[i][j].ToString());
                            }
                            else
                            {
                                surviveAt[j - 1] = int.Parse(substrings[i][j].ToString());
                            }
                        }
                    }
                }
            }
            else
            {
                string[] substrings = rules.Split(',');
                if (substrings.Length != 4)
                {
                    throw new Exception("Invalid rule string - four sections not found");
                }

                bornMin = int.Parse(substrings[0]);
                bornMax = int.Parse(substrings[1]);
                surviveMin = int.Parse(substrings[2]);
                surviveMax = int.Parse(substrings[3]);
            }
        }

        public override void update()
        {
            ReadWriteTexture2D<int> neighborGrid = GraphicsDevice.GetDefault().AllocateReadWriteTexture2D<int>(texture.Width, texture.Height);

            GraphicsDevice.GetDefault().For(texture.Width, texture.Height, new
                NeighborCount(neighborGrid, texture, live, dead, neighborhoodSize));

            if (!largerThanLife)
            {
                ReadWriteBuffer<int> bornAtBuffer = GraphicsDevice.GetDefault().AllocateReadWriteBuffer(bornAt);
                ReadWriteBuffer<int> surviveAtBuffer = GraphicsDevice.GetDefault().AllocateReadWriteBuffer(surviveAt);

                GraphicsDevice.GetDefault().For(texture.Width, texture.Height, new
                    Step(texture, neighborGrid, live, dead, bornAtBuffer, surviveAtBuffer));
            }
            else
            {
                GraphicsDevice.GetDefault().For(texture.Width, texture.Height, new
                    StepLarger(texture, neighborGrid, live, dead, bornMin, bornMax, surviveMin, surviveMax));
            }

            neighborGrid.Dispose();
        }

        [AutoConstructor]
        public readonly partial struct NeighborCount : IComputeShader
        {
            public readonly ReadWriteTexture2D<int> buffer;
            public readonly ReadWriteTexture2D<int> neighborhood;
            public readonly int live;
            public readonly int dead;
            public readonly int zone;

            public void Execute()
            {
                int X = ThreadIds.X;
                int Y = ThreadIds.Y;

                int total = 0;
                for ( //For Y that is not out of bounds
                    int y = Hlsl.Max(0, Y - zone);
                    y <= Hlsl.Min(neighborhood.Height - 1, Y + zone);
                    y++)
                {
                    for ( //For X that is not out of bounds
                        int x = Hlsl.Max(0, X - zone);
                        x <= Hlsl.Min(neighborhood.Width - 1, X + zone);
                        x++)
                    {
                        if (
                            (x != X || y != Y) && //If not the current cell
                            neighborhood[x, y] == live //And cell is alive
                            )
                        {
                            total++;
                        }
                    }
                }

                buffer[ThreadIds.XY] = total;
            }
        }

        [AutoConstructor]
        public readonly partial struct Step : IComputeShader
        {
            public readonly ReadWriteTexture2D<int> buffer;
            public readonly ReadWriteTexture2D<int> neighborCount;
            public readonly int live;
            public readonly int dead;
            public readonly ReadWriteBuffer<int> bornAt;
            public readonly ReadWriteBuffer<int> surviveAt;

            public void Execute()
            {
                int neighbors = neighborCount[ThreadIds.XY];

                for (int i = 0; i < bornAt.Length; i++)
                {
                    if (buffer[ThreadIds.XY] == dead && neighbors == bornAt[i])
                    {
                        buffer[ThreadIds.XY] = live;
                        return;
                    }
                }

                for (int j = 0; j < surviveAt.Length; j++)
                {
                    if (buffer[ThreadIds.XY] == live && neighbors == surviveAt[j])
                    {
                        buffer[ThreadIds.XY] = live;
                        return;
                    }
                }

                buffer[ThreadIds.XY] = dead;
            }
        }

        [AutoConstructor]
        public readonly partial struct StepLarger : IComputeShader
        {
            public readonly ReadWriteTexture2D<int> buffer;
            public readonly ReadWriteTexture2D<int> neighborCount;
            public readonly int live;
            public readonly int dead;
            public readonly int bornMin;
            public readonly int bornMax;
            public readonly int surviveMin;
            public readonly int surviveMax;

            public void Execute()
            {
                int neighbors = neighborCount[ThreadIds.XY];
                
                if (buffer[ThreadIds.XY] == dead && neighbors >= bornMin && neighbors <= bornMax)
                {
                    buffer[ThreadIds.XY] = live;
                    return;
                }

                if (buffer[ThreadIds.XY] == live && neighbors >= surviveMin && neighbors <= surviveMax)
                {
                    buffer[ThreadIds.XY] = live;
                    return;
                }

                buffer[ThreadIds.XY] = dead;
            }
        }

        protected class SBNRuleSet
        {
            public int neighborhoodSize;
            public string ruleString;

            public SBNRuleSet(int neighborhoodSize, string ruleString)
            {
                this.neighborhoodSize = neighborhoodSize;
                this.ruleString = ruleString;
            }
        }
    }
}
