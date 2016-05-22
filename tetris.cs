using System;
using System.Threading;
using System.Collections.Generic;

namespace tetris
{

    class Logic
    {
        public const int windowHeight = 30;
        public const int gameHeight = 25;
        public const int windowWidth = 50;
        public const int gameWidth = 20;
        public const int startingPeriod = 200;
        public const int gamePositionX = 5;
        public const int gamePositionY = 2;
        public static void CreateWindow()
        {
            Console.SetCursorPosition(gamePositionX - 1, gamePositionY);
            for (int i = 0; i <= gameWidth + 1; i++)
                Console.Write((char)0x2550);
            Console.SetCursorPosition(gamePositionX - 1, gameHeight + gamePositionY);
            for (int i = 0; i <= gameWidth + 1; i++)
                Console.Write((char)0x2550);
            for (int j = 0; j < gameHeight; j++)
            {
                Console.SetCursorPosition(gamePositionX - 1, gamePositionY + j);
                Console.Write((char)0x2551);
                Console.SetCursorPosition(gamePositionX + gameWidth, gamePositionY + j);
                Console.Write((char)0x2551);
            }
            Console.SetCursorPosition(gamePositionX - 1, gamePositionY);
            Console.Write((char)0x2554);
            Console.SetCursorPosition(gamePositionX + gameWidth, gamePositionY);
            Console.Write((char)0x2557);
            Console.SetCursorPosition(gamePositionX - 1, gamePositionY + gameHeight);
            Console.Write((char)0x255A);
            Console.SetCursorPosition(gamePositionX + gameWidth, gamePositionY + gameHeight);
            Console.Write((char)0x255D);
        }
        class GameTimer
        {
            public bool Active = true;
            Thread Clock;
            int Period;
            Act Action;
            public GameTimer(int period, Act action)
            {
                Period = period;
                Action = action;
                Clock = new Thread(this.Tick);
                Clock.Start();
            }
            void Tick()
            {
                while (true)
                {
                    Thread.Sleep(Period);
                    Action(null);
                }
            }
            public void ChangePeriod(int period)
            {
                Period = period;
            }
            public void Stop()
            {
                Clock.Abort();
                Active = false;
            }
        }
        public static void Main(string[] args)
        {
            CreateWindow();
            Console.ForegroundColor = ConsoleColor.DarkGray;
            Mutex hold = new Mutex(false,"Console");
            Console.WindowHeight = windowHeight;
            Console.WindowWidth = windowWidth;
            Console.CursorVisible = false;
            Stack gamePlay = new Stack(gamePositionX, gamePositionY, gameHeight, gameWidth);
            NextBlock next = new NextBlock(gamePlay);
            ScoreBoard yourscore = new ScoreBoard();
            gamePlay.setScoreBoard(yourscore);
            Unit Current = null;
            KeyCatch catcher = null;
            GameTimer timer = null;
            Thread mainThread = Thread.CurrentThread;
            int period = startingPeriod;
            int linestonextlevel = 1;
            //bool gameover = false;

            Act gameOver = (par) =>
                {
                    catcher.Stop();
                    timer.Stop();
                    Console.Clear();
                    Console.SetCursorPosition(15, 14);
                    Console.ForegroundColor = ConsoleColor.DarkYellow;
                    Console.WriteLine("GAME OVER");
                    yourscore.Update();
                    hold.Dispose();
                    Console.ReadLine();
                    return par;
                };


            Act newElem = (par) =>
            {
                Current = next.Take();
                if (Current.Collider(0, 0, gamePlay) == 2)
                {
                    hold.ReleaseMutex();
                    Thread q = new Thread(() => gameOver(null));
                    q.Start();
                    Thread.Sleep(500);
                }
                return par;
            };
            Act timerDown = (par) =>
            {
                
                if (Current.Collider(0, 1, gamePlay) != 0)
                {
                    hold.WaitOne();
                    gamePlay.AddUnit(Current);
                    gamePlay.Undisplay();
                    gamePlay.Normalize();
                    if (yourscore.Lines >= linestonextlevel)
                    {
                        yourscore.Increase(0, 1, 0);
                        linestonextlevel += yourscore.Level + 1;
                        period -= 25;
                        timer.ChangePeriod(period);
                    }
                    gamePlay.Display();
                    newElem(null);
                }
                else
                {
                    hold.WaitOne();
                    Current.Undisplay();
                    Current.Move(0, 1);
                    Current.Display();
                }
                hold.ReleaseMutex();
                return par;
            };
            Act gameControl = (par) =>
            {
                
                if (((ConsoleKeyInfo)par).Key == ConsoleKey.UpArrow)
                {
                    hold.WaitOne();
                    Current.Undisplay();
                    Current.Rotate();
                    if (Current.Collider(0, 0, gamePlay) != 0)
                    {
                        Current.Rotate();
                        Current.Rotate();
                        Current.Rotate();
                    }


                    Current.Display();
                    hold.ReleaseMutex();
                }
                if (((ConsoleKeyInfo)par).Key == ConsoleKey.RightArrow)
                {
                    hold.WaitOne();
                    if (Current.Collider(1, 0, gamePlay) == 0)
                    {
                        Current.Undisplay();
                        Current.Move(1, 0);
                        Current.Display();
                    }
                    hold.ReleaseMutex();
                }
                if (((ConsoleKeyInfo)par).Key == ConsoleKey.LeftArrow)
                {
                    hold.WaitOne();
                    if (Current.Collider(-1, 0, gamePlay) == 0)
                    {
                        Current.Undisplay();
                        Current.Move(-1, 0);
                        Current.Display();
                    }
                    hold.ReleaseMutex();
                }
                if (((ConsoleKeyInfo)par).Key == ConsoleKey.DownArrow)
                {
                    hold.WaitOne();
                    if (Current.Collider(0, 1, gamePlay) == 0)
                    {
                        Current.Undisplay();
                        Current.Move(0, 1);
                        Current.Display();
                    }
                    hold.ReleaseMutex();
                }
                if (((ConsoleKeyInfo)par).Key == ConsoleKey.Escape)
                {
                    //timer.Stop();
                    Thread q = new Thread(() => gameOver(null));
                    q.Start();
                    Thread.Sleep(500);
                }
                
                return par;
            };
            catcher = new KeyCatch(gameControl);
            timer = new GameTimer(startingPeriod, timerDown);
            newElem(null);
            
        }
    }

    struct Coord
    {
        public Coord(int _x, int _y)
        {
            x = _x;
            y = _y;
        }
        public int x;
        public int y;
    }
    //базовый класс для всех фигур
    abstract class Unit
    {
        protected char Symbol = (char)0x2588;
        public Coord Base = new Coord(Logic.gameWidth / 2, 2);
        public Coord[] Body;
        virtual public void Display()
        {
            Mutex consl = Mutex.OpenExisting("Console");
            foreach (Coord i in Body)
            {
                consl.WaitOne();
                Console.ForegroundColor = ConsoleColor.DarkGreen;
                Console.SetCursorPosition(Base.x + i.x + Logic.gamePositionX, Base.y + i.y + Logic.gamePositionY);
                Console.Write(Symbol);
                consl.ReleaseMutex();
            }
        }
        virtual public void Undisplay()
        {
            Mutex consl = Mutex.OpenExisting("Console");
            
            foreach (Coord i in Body)
            {
                consl.WaitOne();
                Console.SetCursorPosition(Base.x + i.x + Logic.gamePositionX, Base.y + i.y + Logic.gamePositionY);
                Console.Write(' ');
                consl.ReleaseMutex();
            }
            
        }
        public Unit()
        {

        }

        public void Move(int dx, int dy)
        {
            
            Base.x += dx;
            Base.y += dy;

        }
        public virtual void Rotate()
        {
            
            int temp;
            for (int i = 0; i < Body.Length; i++)
            {
                temp = Body[i].x;
                Body[i].x = Body[i].y;
                Body[i].y = temp;
                Body[i].y *= -1;
            }



        }

        //0 - если фигура может двигаться в данном направлении
        //1 - если фигура не может двигаться в данном направлении, потому что ей мешает стена
        //2 - если фигура не может двигаться в данном направлении, потому что ей мешает куча
        public virtual int Collider(int dx, int dy, Stack temp)
        {
            foreach (Coord b in Body)
            {
                if (((b.y + Base.y + dy) >= Logic.gameHeight) || ((b.x + dx + Base.x) < 0) || ((b.x + dx + Base.x) >= Logic.gameWidth))
                    return 1;
                if (temp.stack[b.y + dy + Base.y][b.x + dx + Base.x]) return 2;
            }
            return 0;
        }
    }

    //Классы-фигурки для тетриса
    class Triangle : Unit
    {
        public Triangle()
        {
            Body = new Coord[] { new Coord(0, 0), new Coord(-1, -1), new Coord(0, -1), new Coord(1, -1) };
        }
    }
    class Square : Unit
    {
        public Square()
        {
            Body = new Coord[] { new Coord(0, 0), new Coord(0, 1), new Coord(1, 1), new Coord(1, 0) };
        }

    }
    class Knight : Unit
    {
        public Knight()
        {
            Body = new Coord[] { new Coord(0, 0), new Coord(1, 0), new Coord(0, 1), new Coord(0, 2) };
        }
    }
    class KnightR : Unit
    {
        public KnightR()
        {
            Body = new Coord[] { new Coord(0, 0), new Coord(-1, 0), new Coord(0, 1), new Coord(0, 2) };
        }
    }
    class Line : Unit
    {
        public Line()
        {
            Body = new Coord[] { new Coord(0, 0), new Coord(0, 1), new Coord(0, 2), new Coord(0, 3) };
        }
    }
    class S : Unit
    {
        public S()
        {
            Body = new Coord[] { new Coord(0, 0), new Coord(1, 0), new Coord(0, 1), new Coord(-1, 1) };
        }
    }
    class Z : Unit
    {
        public Z()
        {
            Body = new Coord[] { new Coord(0, 0), new Coord(-1, 0), new Coord(0, 1), new Coord(1, 1) };
        }
    }
    //Класс-пуля
    class Bullet : Unit
    {
        public Bullet(Coord start)
        {
            Base = start;
            Body = new Coord[] { new Coord(0, 0) };
        }
    }
    //Класс-стрелок
    class Shooter : Unit
    {
        Stack Temp;
        public Shooter(Stack temp)
        {
            Temp = temp;
            Body = new Coord[] { new Coord(0, 0), new Coord(0, 1) };
        }
        override public void Rotate()
        {
            
            Act shooting = (par) =>
            {
                while (((Bullet)par).Collider(0,1,Temp)==0)
                {
                    ((Bullet)par).Move(0, 1);
                    ((Bullet)par).Display();
                    Thread.Sleep(5);
                    ((Bullet)par).Undisplay();
                    
                }
                Temp.AddUnit((Bullet)par);
                Temp.Undisplay();
                Temp.Normalize();
                Temp.Display();
                return par;
            };
            Bullet shot = new Bullet(new Coord(Base.x,Base.y+1));
            /*
            Thread shoot = new Thread(() => shooting(shot));
            shoot.Start();*/
            shooting(shot);
        }
        public override void Display()
        {
            Mutex consl = Mutex.OpenExisting("Console");
            foreach (Coord i in Body)
            {
                consl.WaitOne();
                Console.ForegroundColor = ConsoleColor.Red;
                Console.SetCursorPosition(Base.x + i.x + Logic.gamePositionX, Base.y + i.y + Logic.gamePositionY);
                Console.Write(Symbol);
                Console.ForegroundColor = ConsoleColor.White;
                consl.ReleaseMutex();
            }
        }
    }
    //Класс-бомба
    class Bomb : Unit
    {
        Stack Temp;
        bool explode = false;
        public Bomb (Stack temp) {
            Body = new Coord[] {
                new Coord(0,0)          
        };
            Temp = temp;
    }
        public override void Rotate()
        {
            Temp.Undisplay();
            Temp.AddUnit(this);
            Temp.Normalize();
            Temp.Display();
            Body = new Coord[] { };
            explode = true;
        }
        public override void Display()
        {
            Mutex consl = Mutex.OpenExisting("Console");
            foreach (Coord i in Body)
            {
                consl.WaitOne();
                Console.ForegroundColor = ConsoleColor.Blue;
                Console.SetCursorPosition(Base.x + i.x + Logic.gamePositionX, Base.y + i.y + Logic.gamePositionY);
                Console.Write(Symbol);
                Console.ForegroundColor = ConsoleColor.White;
                consl.ReleaseMutex();
            }
        }
        public override void Undisplay()
        {
            
            if (base.Collider(0, 0, Temp) == 2)
                Temp.Display();
            else
            base.Undisplay();
        }
        public override int Collider(int dx, int dy, Stack temp)
        {
            if (explode) return 2;
            int retValue = base.Collider(dx, dy, temp);
            return (retValue == 1) ? 1 : 0;
        }
    }

    class Stack
    {
        ScoreBoard scoreboard;
        public bool[][] stack;
        int Height;
        int Width;
        int X;
        int Y;
        public Stack(int x, int y, int height, int width)
        {
            Height = height;
            Width = width;
            X = x;
            Y = y;
            stack = new bool[height][];
            for (int i = 0; i < height; i++)
                stack[i] = new bool[width];

        }
        public void Display()
        {
            Mutex consl = Mutex.OpenExisting("Console");
            
            for (int i = 0; i < Height; i++)
                for (int j = 0; j < Width; j++)
                {
                    if (stack[i][j])
                    {
                        consl.WaitOne();
                        Console.ForegroundColor = ConsoleColor.DarkGray;
                        Console.SetCursorPosition(X + j, Y + i);
                        Console.Write((char)0x2588);
                        consl.ReleaseMutex();
                    }

                }
        }
        public void Undisplay()
        {
            Mutex consl = Mutex.OpenExisting("Console");
            for (int i = 0; i < Height; i++)
                for (int j = 0; j < Width; j++)
                {
                    if (stack[i][j])
                    {
                        consl.WaitOne();
                        Console.SetCursorPosition(X + j, Y + i);
                        Console.Write(' ');
                        consl.ReleaseMutex();
                    }

                }
        }
        void Swap(int a, int b)
        {
            var temp = stack[a];
            stack[a] = stack[b];
            stack[b] = temp;
        }
        bool Full(int a)
        {
            for (int q = 0; q < Width; q++)
                if (!stack[a][q]) return false;
            return true;
        }
        bool Empty(int a)
        {
            for (int q = 0; q < Width; q++)
                if (stack[a][q]) return false;
            return true;
        }
        void Erase(int a)
        {
            for (int i = 0; i < Width; i++)
                stack[a][i] = false;
        }
        public void Normalize()
        {
            int koef = 1;
            int i = Height - 1;
            while (!Empty(i))
            {
                if (Full(i))
                {
                    Erase(i);
                    int j = i - 1;
                    while (!Empty(j))
                    {
                        Swap(j, j + 1);
                        j--;
                    }
                    i++;
                    scoreboard.Increase(koef * 150,0,1);
                    koef++;
                }
                i--;
            }
        }
        public void AddUnit(Unit G)
        {
            foreach (Coord b in G.Body)
            {
                stack[b.y + G.Base.y][b.x + G.Base.x] = true;
            }
        }
        public void AddBomb(Unit G)
        {
            bool cond;
            foreach (Coord b in G.Body)
            {
                cond = ((b.y + G.Base.y + 1) < Height) && ((b.y + G.Base.y + 1) >= 0);
                cond = cond && ((b.x + G.Base.x + 1) < Width) && ((b.x + G.Base.x + 1) >= 0);
                if (cond)
                stack[b.y + G.Base.y + 1][b.x + G.Base.x + 1] = false;

                cond = ((b.y + G.Base.y - 1) < Height) && ((b.y + G.Base.y - 1) >= 0);
                cond = cond && ((b.x + G.Base.x + 1) < Width) && ((b.x + G.Base.x + 1) >= 0);
                if (cond)
                stack[b.y + G.Base.y - 1][b.x + G.Base.x + 1] = false;

                cond = ((b.y + G.Base.y + 1) < Height) && ((b.y + G.Base.y + 1) >= 0);
                cond = cond && ((b.x + G.Base.x - 1) < Width) && ((b.x + G.Base.x - 1) >= 0);
                if (cond)
                stack[b.y + G.Base.y + 1][b.x + G.Base.x - 1] = false;

                cond = ((b.y + G.Base.y - 1) < Height) && ((b.y + G.Base.y - 1) >= 0);
                cond = cond && ((b.x + G.Base.x - 1) < Width) && ((b.x + G.Base.x - 1) >= 0);
                if (cond)
                stack[b.y + G.Base.y - 1][b.x + G.Base.x - 1] = false;
            }
        }
        public void setScoreBoard(ScoreBoard toScore)
        {
            scoreboard = toScore;
            scoreboard.Update();
        }
    }

    class KeyCatch
    {
        Thread readKeyThread;
        public KeyCatch(Act KeyHandler)
        {
            readKeyThread = new Thread(() => readKey(KeyHandler));
            readKeyThread.Start();
        }
        void readKey(Act KeyHandler)
        {
            ConsoleKeyInfo keyPressed;
            while (true)
            {
                keyPressed = Console.ReadKey(true);
                KeyHandler(keyPressed);
            }
        }
        public void Stop()
        {
            readKeyThread.Abort();
        }

    }

    class ScoreBoard
    {
        public int Score = 0;
        public int Level = 1;
        public int Lines = 0;
        public void Update()
        {
            Mutex consl = Mutex.OpenExisting("Console");
            consl.WaitOne();
            Console.ForegroundColor = ConsoleColor.Gray;
            Console.SetCursorPosition(30, 17);
            Console.Write("{0,7}","СЧЕТ");
            Console.SetCursorPosition(30, 18);
            Console.Write("{0,7}", Score);
            Console.SetCursorPosition(30, 19);
            Console.Write("{0,7}", "СТРОКИ");
            Console.SetCursorPosition(30, 20);
            Console.Write("{0,7}", Lines);
            Console.SetCursorPosition(30, 21);
            Console.Write("{0,7}", "УРОВЕНЬ");
            Console.SetCursorPosition(30, 22);
            Console.Write("{0,7}", Level);
            consl.ReleaseMutex();
        }
        public void Increase(int scr, int lev, int lin)
        {
            Score += scr;
            Level += lev;
            Lines += lin;
            Update();
        }
    }

    class NextBlock
    {
        Unit next;
        Random rand;
        Stack mystack;
        public NextBlock(Stack stack)
        {
            mystack = stack;
            rand = new Random((int)DateTime.Now.Ticks);
            next = NewElement(rand.Next(0, 9)); 
            showBlock();
        }
        public void showBlock()
        {
            Mutex consl = Mutex.OpenExisting("Console");
            foreach (Coord i in next.Body)
            {
                consl.WaitOne();
                Console.ForegroundColor = ConsoleColor.DarkYellow;
                Console.SetCursorPosition(23 + i.x + Logic.gamePositionX, 5 + i.y + Logic.gamePositionY);
                Console.Write((char)0x2588);
                consl.ReleaseMutex();
            }
        }
        public void unshowBlock()
        {
            Mutex consl = Mutex.OpenExisting("Console");
            foreach (Coord i in next.Body)
            {
                consl.WaitOne();
                Console.SetCursorPosition(23 + i.x + Logic.gamePositionX, 5 + i.y + Logic.gamePositionY);
                Console.Write(' ');
                consl.ReleaseMutex();
            }
        }
        Unit NewElement(int rand)
        {
            
           
            switch (rand)
            {
                case 0:
                    return new Triangle();
                case 1:
                    return new Square();
                case 2:
                    return new Line();
                case 3:
                    return new Knight();
                case 4:
                    return new KnightR();
                case 5:
                    return new S();
                case 6:
                    return new Z();
                case 7:
                    return new Shooter(mystack);
                default:
                    return new Bomb(mystack);
            }
        }
        public Unit Take()
        {
            Unit temp = next;
            unshowBlock();
            next = NewElement(rand.Next(0,9));
            showBlock();
            return temp;
        }
    }

    delegate object Act(object parameter);
}
