using DriftLab.Core;
using System.Drawing.Drawing2D;
namespace DriftLab.Desktop;
public sealed class StickView:Control
{
 private StickSample? sample;private readonly bool right;public StickView(bool right){this.right=right;DoubleBuffered=true;Dock=DockStyle.Fill;BackColor=Color.FromArgb(20,27,38);}
 public void UpdateSample(StickSample? value){sample=value;Invalidate();}
 protected override void OnPaint(PaintEventArgs e){base.OnPaint(e);var g=e.Graphics;g.SmoothingMode=SmoothingMode.AntiAlias;float radius=Math.Max(20,Math.Min(Width,Height-65)/2f-18),cx=Width/2f,cy=Height/2f-10;
  using var grid=new Pen(Color.FromArgb(60,76,94));using var path=new Pen(Color.FromArgb(40,213,188),2);using var font=new Font("Segoe UI",11);using var text=new SolidBrush(Color.Gainsboro);using var dot=new SolidBrush(Color.FromArgb(40,213,188));
  g.DrawEllipse(grid,cx-radius,cy-radius,radius*2,radius*2);g.DrawLine(grid,cx-radius,cy,cx+radius,cy);g.DrawLine(grid,cx,cy-radius,cx,cy+radius);g.DrawEllipse(path,cx-radius*.02f,cy-radius*.02f,radius*.04f,radius*.04f);
  double x=sample==null?0:right?sample.Value.RX:sample.Value.LX,y=sample==null?0:right?sample.Value.RY:sample.Value.LY;g.FillEllipse(dot,cx+(float)x*radius-5,cy+(float)y*radius-5,10,10);
  g.DrawString(right?AppText.T("STICK DESTRO"):AppText.T("STICK SINISTRO"),font,text,12,8);g.DrawString(sample==null?AppText.T("Nessun dato"):$"X {x*100:+0.00;-0.00;0.00}%    Y {y*100:+0.00;-0.00;0.00}%",font,text,12,Height-35);
 }
}
public sealed class TimelineView:Control
{
 private readonly Queue<StickSample> points=[];private TimeSpan last;public TimelineView(){DoubleBuffered=true;Dock=DockStyle.Fill;BackColor=Color.FromArgb(20,27,38);}
 public void Clear(){points.Clear();last=default;Invalidate();}
 public void UpdateSample(StickSample? sample){if(sample==null||sample.Value.Time==last)return;var s=sample.Value;if(s.Time<last)points.Clear();last=s.Time;points.Enqueue(s);while(points.Count>0&&(s.Time-points.Peek().Time).TotalSeconds>12)points.Dequeue();Invalidate();}
 protected override void OnPaint(PaintEventArgs e){base.OnPaint(e);using var pen=new Pen(Color.FromArgb(40,213,188),1.6f);using var grid=new Pen(Color.FromArgb(60,76,94));using var brush=new SolidBrush(Color.Gainsboro);e.Graphics.DrawString(AppText.T("Y sinistro · ultimi 12 s · scala ±15% (valori più grandi tagliati solo nel grafico)"),Font,brush,10,6);float mid=Height/2f+10;e.Graphics.DrawLine(grid,0,mid,Width,mid);if(points.Count<2)return;var data=points.Select(s=>new PointF((float)(Width*(1-(last-s.Time).TotalSeconds/12)),mid-(float)Math.Clamp(s.LY/.15,-1,1)*(Height/2f-35))).ToArray();e.Graphics.DrawLines(pen,data);}
}
