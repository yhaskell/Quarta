using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Quarta
{
    public class IntPoint
    {
        public int X, Y;

        public int Data;
    }
    
    class GameLogic
    {
        public int[,] Field { get; private set; }

        public Random seed { get; private set; }

        public int FieldSize { get; private set; }

        public int MaxColor { get; private set; }

        private int m_points;

        public int Score { get { return m_points; } private set { m_points = value; ScoreChanged(this, m_points); } }
       
        public void Hint(out int x1, out int y1, out int x2, out int y2)
        {
            for (int i = 0; i < FieldSize; i++)
                for (int j = 0; j < FieldSize; j++)
                    for (int k = i + 1; k < FieldSize; k++)
                        if (Field[i, j] == Field[k, j])
                            for (int l = j + 1; l < FieldSize; l++)
                                if (Field[i, j] == Field[i, l] && Field[k, j] == Field[k, l])
                                {
                                    x1 = i; y1 = j;
                                    x2 = k; y2 = l;
                                    Score -= 10;
                                    return;
                                }
            x1 = y1 = x2 = y2 = -1;
        }

        public int Available 
        {
            get
            {
                int result = 0;
                for (int i = 0; i < FieldSize; i++)
                    for (int j = 0; j < FieldSize; j++)
                        for (int k = i + 1; k < FieldSize; k++)
                            if (Field[i, j] == Field[k, j])
                                for (int l = j + 1; l < FieldSize; l++)
                                    if (Field[i, j] == Field[i, l] && Field[k, j] == Field[k, l])
                                        result++;

                return result;
            }
        }

        private void Shift(int i, int j)
        {
            Field[i, j] = seed.Next(0, MaxColor);
            Changed.Invoke(this, new IntPoint { X = i, Y = j, Data = Field[i, j] });
        }

        public void Shuffle(bool Timed = false)
        {
            for (int i = 0; i < FieldSize; i++) for (int j = 0; j < FieldSize; j++) Shift(i, j);
            if (!Timed && Available > 0) Score -= Score / 10;
        }

        public void Swap<T>(ref T x, ref T y) { T temp = x; x = y; y = temp; }

        public bool Remove(int x1, int y1, int x2, int y2) {
            if (Field[x1, y1] != Field[x1, y2] || Field[x1, y1] != Field[x2, y1] || Field[x1, y1] != Field[x2, y2]) return false;

            if (x1 > x2) Swap(ref x1, ref x2);
            if (y1 > y2) Swap(ref y1, ref y2);

            var sc = (x2 - x1) * (y2 - y1);

            if (sc < 20) Score += 10 * sc;
            else Score += 10 * sc + (sc - 10) * (sc - 10);
            for (int i = x1; i <= x2; i++) for (int j = y1; j <= y2; j++) Shift(i, j);
            if (Available == 0) NoMoves.Invoke(this, new EventArgs());
            return true;
        }

        public void Start()
        {
            if (!CanStart) throw new ArgumentException("Cannot start unitialized game");
            Shuffle(true);
            Score = 0;
        }
            

        bool CanStart = false;

        public GameLogic() { seed = new Random(); }

        public void Init(int sz, int cm)
        {
            FieldSize = sz; 
            MaxColor = cm; 
            Field = new int[sz, sz];
            CanStart = true;
        }

        bool Continuing = false;

        public string Save()
        {
            if (Continuing) throw new Exception("Saving while continuing is prohibited.");
            string result = Score.ToString() + ":" + FieldSize.ToString() + ":" + MaxColor.ToString() + ":";
            for (int i = 0; i < FieldSize; i++) 
                for (int j = 0; j < FieldSize; j++) 
                    result += Field[i, j].ToString();
            return result;
        }
        
        public void Load(string saveData)
        {
            var d = saveData.Split(':');
            Score = int.Parse(d[0]);
            FieldSize = int.Parse(d[1]);
            MaxColor = int.Parse(d[2]);
            Field = new int[FieldSize, FieldSize];
            CanStart = true;
            for (int i = 0; i < FieldSize; i++) 
                for (int j = 0; j < FieldSize; j++) 
                    Field[i, j] = d[3][i * FieldSize + j] - '0';         
        }

        public void Continue()
        {
            for (int i = 0; i < FieldSize; i++) 
                for (int j = 0; j < FieldSize; j++) 
                    Changed.Invoke(this, new IntPoint { X = i, Y = j, Data = Field[i, j] });
        }

        public void Restart()
        {
            Start();
        }

        public event EventHandler<IntPoint> Changed;
        public event EventHandler<int> ScoreChanged;
        public event EventHandler NoMoves;

    }
}
