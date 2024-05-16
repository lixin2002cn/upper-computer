using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Drawing;
using System.Windows.Forms;

namespace shangweiji
{
    public class AutoSizeFormClass
    {
        public static float X;
        public static float Y;
        public static void setTag(Control cons)
        {
            foreach (Control con in cons.Controls)
            {

                con.Tag = con.Width + ":"
                    + con.Height + ":"
                    + con.Left + ":"
                    + con.Top + ":"
                    + con.Font.Size;
                if (con.Controls.Count > 0)
                {
                    setTag(con);//递归遍历完所有的control
                }
            }
        }

        public static void setControls(float newx, float newy, Control cons)
        {
            foreach (Control con in cons.Controls)
            {
                if (con.Tag != null)
                {
                    string[] mytag = con.Tag.ToString().Split(new char[] { ':' });
                    float a = Convert.ToSingle(mytag[0]) * newx;
                    con.Width = (int)a;
                    a = Convert.ToSingle(mytag[1]) * newx;
                    con.Height = (int)a;
                    a = Convert.ToSingle(mytag[2]) * newx;
                    con.Left = (int)a;
                    a = Convert.ToSingle(mytag[3]) * newx;
                    con.Top = (int)a;
                    Single currentSize = Convert.ToSingle(mytag[4]) * Math.Min(newx, newy);
                    con.Font = new System.Drawing.Font(con.Font.Name, currentSize, con.Font.Style, con.Font.Unit);
                    if (con.Controls.Count > 0)
                    {
                        setControls(newx, newy, con);//递归遍历所有的control
                    }
                }


            }
        }




    }

}
