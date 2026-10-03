using System.Drawing.Drawing2D;
using System.Drawing.Text;
using System.Globalization;
using System.Reflection;
using System.Text.Json;
using System.Runtime.InteropServices;
using Microsoft.Win32;
namespace Unterrichtszeiten;
internal static class Program
{
    [STAThread] static void Main(){ApplicationConfiguration.Initialize();Application.Run(new ClockWindow());}
}
internal sealed class Preferences
{
    public bool Full {get;set;}
    public bool Pin {get;set;}
    public string Theme {get;set;}="auto";
    public Rectangle? Small {get;set;}
    public Rectangle? Large {get;set;}
}
internal sealed class ClockWindow:Form
{
    private readonly string root=Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),"BBS Einbeck","Unterrichtszeiten-Native");
    private Preferences prefs=new();
    private readonly SchoolClock school;
    private readonly PtbClock clock=new();
    private readonly CancellationTokenSource lifetime=new();
    private readonly System.Windows.Forms.Timer tick=new(){Interval=250};
    private readonly System.Windows.Forms.Timer sync=new(){Interval=300000};
    private readonly ToolStrip bar=new(){Dock=DockStyle.Top,GripStyle=ToolStripGripStyle.Hidden};
    private readonly ToolStripButton pin=new("Anheften"){CheckOnClick=true,ToolTipText="Immer im Vordergrund"};
    private readonly ToolStripButton view=new("Tagesübersicht");
    private readonly ToolTip webTip=new();
    private RectangleF webBounds;
    private const string WebHint="Web-Version / Downloads öffnen";
    private readonly Canvas canvas=new(){Dock=DockStyle.Fill};
    private readonly Image logo,logoDark;
    private readonly List<ToolStripMenuItem> choices=[];
    private bool syncing,dark,adjusting,rounding,firstPaint=true;
    private Size roundedSize;
    private int roundedDpi;
    private readonly ToolStripLabel dragTitle=new("Unterrichtszeit") { ToolTipText="Hier ziehen, um das Fenster zu verschieben", AutoSize=false, Width=108 };
    [DllImport("user32.dll")] private static extern bool ReleaseCapture();
    [DllImport("user32.dll")] private static extern IntPtr SendMessage(IntPtr handle,int message,IntPtr wParam,IntPtr lParam);
    private void DragWindow(object? sender,MouseEventArgs e)
    {
        if(e.Button!=MouseButtons.Left)return;
        ReleaseCapture();SendMessage(Handle,0xA1,(IntPtr)2,IntPtr.Zero);
    }
    private int DpiPixels(int value)=>(int)Math.Round(value*DeviceDpi/96d);
    protected override void OnHandleCreated(EventArgs e)
    {
        base.OnHandleCreated(e);RoundWindow();
    }
    private void RoundWindow()
    {
        if(rounding||ClientSize.Width<24||ClientSize.Height<24||WindowState==FormWindowState.Minimized)return;
        var size=ClientSize;int dpi=DeviceDpi;
        if(size==roundedSize&&dpi==roundedDpi)return;
        rounding=true;
        try {
            float d=DpiPixels(24),w=size.Width,h=size.Height;
            using var path=new GraphicsPath();
            path.AddArc(0,0,d,d,180,90);path.AddArc(w-d,0,d,d,270,90);
            path.AddArc(w-d,h-d,d,d,0,90);path.AddArc(0,h-d,d,d,90,90);path.CloseFigure();
            // SetWindowRgn may synchronously trigger Resize again. Cache before assigning.
            roundedSize=size;roundedDpi=dpi;
            var previous=Region;Region=new Region(path);previous?.Dispose();
        } finally {rounding=false;}
    }
    private void WindowLimits()
    {
        MaximumSize=Size.Empty;
        MinimumSize=prefs.Full?new Size(DpiPixels(760),DpiPixels(580)):new Size(DpiPixels(600),DpiPixels(200));
        if(!prefs.Full)MaximumSize=new Size(DpiPixels(760),DpiPixels(400));
    }
    protected override void WndProc(ref Message m)
    {
        if(m.Msg==0x112&&(m.WParam.ToInt64()&0xfff0)==0xf030&&!prefs.Full)return;
        if(m.Msg==0x84&&WindowState==FormWindowState.Normal)
        {
            var point=PointToClient(new Point(unchecked((short)(m.LParam.ToInt64()&0xffff)),unchecked((short)((m.LParam.ToInt64()>>16)&0xffff))));
            int edge=Math.Max(4,DeviceDpi/24);bool left=point.X<edge,right=point.X>=ClientSize.Width-edge,top=point.Y<edge,bottom=point.Y>=ClientSize.Height-edge;
            int hit=top?(left?13:right?14:12):bottom?(left?16:right?17:15):left?10:right?11:0;
            if(!prefs.Full)hit=left?10:right?11:0;
            if(hit!=0){m.Result=(IntPtr)hit;return;}
        }
        base.WndProc(ref m);
    }
    private void FitCompact()
    {
        if(prefs.Full||adjusting||WindowState!=FormWindowState.Normal||IsDisposed)return;
        adjusting=true;
        try {
        int limitedWidth=Math.Clamp(ClientSize.Width,DpiPixels(600),DpiPixels(760));
        if(ClientSize.Width!=limitedWidth)ClientSize=new Size(limitedWidth,ClientSize.Height);
        float designHeight=school.Evaluate(clock.Now).Remaining==null?200:232;
        int desired=(int)Math.Ceiling((ClientSize.Width-Padding.Horizontal)*designHeight/520)+bar.Height+Padding.Vertical;
        if(ClientSize.Height!=desired)ClientSize=new Size(ClientSize.Width,desired);
        } finally {adjusting=false;}
    }
    private static readonly Color Yellow=Color.FromArgb(245,214,0),Ink=Color.FromArgb(32,35,34),Cream=Color.FromArgb(255,249,230);
    private string Settings=>Path.Combine(root,"window.json");
    private static Stream Resource(string name)=>Assembly.GetExecutingAssembly().GetManifestResourceStream(name)!;
    private static Image LoadImage(string name){using var stream=Resource(name);using var im=Image.FromStream(stream);return new Bitmap(im);}
    public ClockWindow()
    {
        
        using(var reader=new StreamReader(Resource("calendar.json")))school=new SchoolClock(reader.ReadToEnd());
        
        logo=LoadImage("logo.png");logoDark=LoadImage("logo-dark.png");
        
        using(var stream=Resource("app.ico"))Icon=new Icon(stream);
        Text="Unterrichtszeit · BBS Einbeck";
        FormBorderStyle=FormBorderStyle.None;Padding=new Padding(4);
        bar.AutoSize=false;bar.Height=30;bar.CanOverflow=false;
        bar.Renderer=new PolishedRenderer();
        
        try{prefs=JsonSerializer.Deserialize<Preferences>(File.ReadAllText(Settings))??new();}catch{}
        
        if(prefs.Theme is not("auto" or "light" or "dark"))prefs.Theme="auto";
        var themes=new ToolStripDropDownButton("Ansicht");
        foreach(var (name,value) in new[]{("Automatisch","auto"),("Hell","light"),("Dunkel","dark")})
        {var item=new ToolStripMenuItem(name){Tag=value};item.Click+=(_,_)=>{prefs.Theme=value;ApplyTheme();Save();};themes.DropDownItems.Add(item);choices.Add(item);}
        var close=new ToolStripButton("×"){Name="close",Alignment=ToolStripItemAlignment.Right,ToolTipText="Schließen (Alt+F4)",AccessibleName="Schließen",AutoSize=false,Width=32};
        var minimize=new ToolStripButton("−"){Alignment=ToolStripItemAlignment.Right,ToolTipText="Minimieren",AccessibleName="Minimieren",AutoSize=false,Width=32};
        close.Click+=(_,_)=>Close();minimize.Click+=(_,_)=>WindowState=FormWindowState.Minimized;
        dragTitle.MouseDown+=DragWindow;
        bar.MouseDown+=(sender,e)=>{if(bar.GetItemAt(e.Location)==null)DragWindow(sender,e);};
        bar.Items.AddRange([dragTitle,new ToolStripSeparator(),pin,view,themes,close,minimize]);
        Controls.Add(canvas);Controls.Add(bar);
        pin.Checked=prefs.Pin;TopMost=prefs.Pin;UpdatePin();
        pin.CheckedChanged+=(_,_)=>{prefs.Pin=pin.Checked;TopMost=prefs.Pin;UpdatePin();Save();};
        view.Click+=(_,_)=>{Remember();prefs.Full=!prefs.Full;SetBounds();Save();canvas.Invalidate();};
        canvas.Paint+=PaintClock;
        canvas.MouseMove+=(_,e)=>{
            bool over=webBounds.Contains(e.Location);
            canvas.Cursor=over?Cursors.Hand:Cursors.Default;
            string hint=over?WebHint:"";
            if(webTip.GetToolTip(canvas)!=hint)webTip.SetToolTip(canvas,hint);
        };
        canvas.MouseLeave+=(_,_)=>{canvas.Cursor=Cursors.Default;webTip.SetToolTip(canvas,"");};
        canvas.MouseClick+=(_,e)=>{
            if(e.Button==MouseButtons.Left&&webBounds.Contains(e.Location))
                try { System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo("https://apps.bbs-einbeck.de/uzeit/"){UseShellExecute=true}); }
                catch(Exception ex) { MessageBox.Show(this,"Der Browser konnte nicht geöffnet werden.\n"+ex.Message,"Unterrichtszeit",MessageBoxButtons.OK,MessageBoxIcon.Information); }
        };
        tick.Tick+=(_,_)=>{FitCompact();canvas.Invalidate();};sync.Tick+=async(_,_)=>await Sync();
        Activated+=(_,_)=>ApplyTheme();
        
        SystemEvents.UserPreferenceChanged+=SystemPreferenceChanged;
        Shown+=(_,_)=>{
            tick.Start();
            // Network initialization starts only after the first completed paint.
        };
        FormClosing+=(_,_)=>{Remember();Save();lifetime.Cancel();};
        Resize+=(_,_)=>{FitCompact();RoundWindow();};
        DpiChanged+=(_,_)=>{WindowLimits();FitCompact();RoundWindow();};
        ApplyTheme();
        SetBounds();FitCompact();RoundWindow();
    }
    private void UpdatePin()
    {
        pin.Text=pin.Checked?"Angeheftet":"Anheften";
        pin.AccessibleName=pin.Checked?"Immer im Vordergrund aktiviert":"Immer im Vordergrund aktivieren";
        pin.ToolTipText=pin.Checked?"Anheften aufheben":"Immer im Vordergrund";bar.Invalidate();
    }
    private void SystemPreferenceChanged(object sender,UserPreferenceChangedEventArgs e){if(!IsDisposed&&IsHandleCreated)BeginInvoke(new Action(ApplyTheme));}
    private void ApplyTheme()
    {
        bool systemDark=false;try{using var key=Registry.CurrentUser.OpenSubKey(@"Software\Microsoft\Windows\CurrentVersion\Themes\Personalize");systemDark=key?.GetValue("AppsUseLightTheme") is int v&&v==0;}catch{}
        dark=prefs.Theme=="dark"||(prefs.Theme=="auto"&&systemDark);
        BackColor=canvas.BackColor=dark?Color.FromArgb(20,22,21):Color.FromArgb(246,246,243);
        bar.BackColor=dark?Ink:Color.WhiteSmoke;bar.ForeColor=dark?Cream:Ink;
        foreach(var item in choices)item.Checked=(string)item.Tag! ==prefs.Theme;
        canvas.Invalidate();
    }
    private async Task Sync(){if(syncing||lifetime.IsCancellationRequested)return;syncing=true;try{await Task.Run(()=>clock.Sync(lifetime.Token));if(!IsDisposed)canvas.Invalidate();}finally{syncing=false;}}
    private void Remember(){var r=WindowState==FormWindowState.Normal?Bounds:RestoreBounds;if(prefs.Full)prefs.Large=r;else prefs.Small=r;}
    private void SetBounds()
    {
        WindowState=FormWindowState.Normal;WindowLimits();
        StartPosition=FormStartPosition.Manual;var r=prefs.Full?prefs.Large:prefs.Small;
        var area=r.HasValue?Screen.FromRectangle(r.Value).WorkingArea:Screen.FromControl(this).WorkingArea;
        if(r.HasValue){int requestedWidth=!prefs.Full&&(r.Value.Width>DpiPixels(760)||r.Value.Width<DpiPixels(600))?DpiPixels(640):r.Value.Width;int w=Math.Clamp(requestedWidth,MinimumSize.Width,prefs.Full?Math.Max(MinimumSize.Width,area.Width):DpiPixels(760)),h=Math.Clamp(r.Value.Height,MinimumSize.Height,Math.Max(MinimumSize.Height,area.Height));Bounds=new Rectangle(Math.Clamp(r.Value.X,area.Left,Math.Max(area.Left,area.Right-w)),Math.Clamp(r.Value.Y,area.Top,Math.Max(area.Top,area.Bottom-h)),w,h);}
        else{ClientSize=prefs.Full?new Size(DpiPixels(1000),DpiPixels(710)):new Size(DpiPixels(640),DpiPixels(270));Size=new Size(Math.Min(Width,area.Width),Math.Min(Height,area.Height));Location=new Point(area.Left+(area.Width-Width)/2,area.Top+(area.Height-Height)/2);}
        view.Text=prefs.Full?"Kompakt":"Schultag";view.ToolTipText=prefs.Full?"Zur kleinen Uhr wechseln":"Vollständige Tagesübersicht öffnen";
        FitCompact();
    }
    private void Save(){try{Directory.CreateDirectory(root);var temp=Settings+"."+Guid.NewGuid().ToString("N")+".tmp";File.WriteAllText(temp,JsonSerializer.Serialize(prefs));File.Move(temp,Settings,true);}catch(IOException){}catch(UnauthorizedAccessException){}}
    private static void Box(Graphics g,Color color,float x,float y,float w,float h,float radius=10)
    {
        using var brush=new SolidBrush(color);using var path=new GraphicsPath();float d=radius*2;
        path.AddArc(x,y,d,d,180,90);path.AddArc(x+w-d,y,d,d,270,90);path.AddArc(x+w-d,y+h-d,d,d,0,90);path.AddArc(x,y+h-d,d,d,90,90);path.CloseFigure();g.FillPath(brush,path);
    }
    private static void TextAt(Graphics g,string text,float size,Color color,RectangleF box,bool bold=false,StringAlignment align=StringAlignment.Near)
    {
        using var font=new Font("Segoe UI",size,bold?FontStyle.Bold:FontStyle.Regular,GraphicsUnit.Pixel);using var brush=new SolidBrush(color);using var format=new StringFormat{Alignment=align,LineAlignment=StringAlignment.Center,Trimming=StringTrimming.EllipsisCharacter,FormatFlags=StringFormatFlags.NoWrap};g.DrawString(text,font,brush,box,format);
    }
    private void PaintClock(object? sender,PaintEventArgs e)
    {
        RenderClock(e);
        if(firstPaint) {
            firstPaint=false;
            BeginInvoke(new Action(()=>{if(!IsDisposed){sync.Start();_ = Sync();}}));
        }
    }
    private void RenderClock(PaintEventArgs e)
    {
        var g=e.Graphics;g.SmoothingMode=SmoothingMode.HighQuality;g.PixelOffsetMode=PixelOffsetMode.HighQuality;
        g.TextRenderingHint=TextRenderingHint.AntiAliasGridFit;g.InterpolationMode=InterpolationMode.HighQualityBicubic;var state=school.Evaluate(clock.Now);
        float designW=prefs.Full?960:520,designH=prefs.Full?660:state.Remaining==null?200:232;
        float scale=Math.Min(canvas.ClientSize.Width/designW,canvas.ClientSize.Height/designH);
        if(scale<=0)return;g.TranslateTransform((canvas.ClientSize.Width-designW*scale)/2,0);g.ScaleTransform(scale,scale);
        if(!prefs.Full){DrawCards(g,state,0,0,1);return;}
        var fg=dark?Cream:Ink;g.DrawImage(dark?logoDark:logo,new RectangleF(20,16,180,49));
        TextAt(g,state.Time.ToString("dddd, dd. MMMM yyyy",CultureInfo.GetCultureInfo("de-DE")),14,dark?Color.Silver:Color.DimGray,new(230,18,700,40),align:StringAlignment.Far);
        TextAt(g,"Alles zu seiner Zeit.",26,fg,new(20,76,700,45),true);
        DrawCards(g,state,12,145,1.04f);
        Box(g,dark?Ink:Color.White,570,133,372,480);
        TextAt(g,"Dein Schultag",21,fg,new(587,145,340,30),true);
        for(int i=0;i<SchoolClock.Slots.Length;i++)
        {
            var s=SchoolClock.Slots[i];float y=184+i*26;bool active=i==state.Index;
            if(active||s.Pause)Box(g,active?(dark?Yellow:Ink):(dark?Color.FromArgb(43,47,44):Color.FromArgb(243,245,239)),582,y,348,24,5);
            var color=active?(dark?Ink:Yellow):fg;
            TextAt(g,s.Name+(s.Start==675?" · Mensazeit":""),12,color,new(591,y,177,24),active);
            TextAt(g,$"{SchoolClock.Hm(s.Start)} – {SchoolClock.Hm(s.End)}",12,color,new(770,y,150,24),align:StringAlignment.Far);
        }
        TextAt(g,state.Calendar,11,dark?Color.Silver:Color.DimGray,new(588,558,338,35));
        TextAt(g,"© Marc Schulz · marc.schulz@bbs-einbeck.de",12,fg,new(20,612,900,24));
        TextAt(g,"Windows-Version des Visual-Basic-Unterrichtsprojektes aus 2012",11,dark?Color.Silver:Color.DimGray,new(20,636,900,20));
    }
    private void DrawCards(Graphics g,ClockState s,float x,float y,float scale)
    {
        var saved=g.Save();g.TranslateTransform(x,y);g.ScaleTransform(scale,scale);
        var corners=new[]{new PointF(8,8),new PointF(512,120)};
        using(var transform=g.Transform)transform.TransformPoints(corners);
        webBounds=RectangleF.FromLTRB(corners[0].X,corners[0].Y,corners[1].X,corners[1].Y);
        Box(g,Ink,8,8,504,112);
        string digits=s.Time.ToString("HHmm");
        for(int i=0;i<4;i++)
        {
            float dx=22+i*49+(i>=2?10:0);Box(g,Color.FromArgb(46,49,47),dx,28,43,69,4);
            TextAt(g,digits[i].ToString(),53,Cream,new(dx,24,43,74),true,StringAlignment.Center);
            using var pen=new Pen(Color.FromArgb(15,18,16),1);g.DrawLine(pen,dx,62,dx+43,62);
        }
        if(!SystemInformation.UIEffectsEnabled||s.Time.Millisecond<500)TextAt(g,":",36,Yellow,new(114,24,16,70),true,StringAlignment.Center);
        TextAt(g,clock.Source,10,Color.Silver,new(385,13,110,20),align:StringAlignment.Far);
        TextAt(g,"aktuell:",12,Color.Silver,new(270,29,110,18));
        var shortStatus=s.Status=="Unterrichtsfreie Zeit"?(s.Remaining!=null?"Guten Morgen":"Feierabend"):s.Status.Contains("ferien",StringComparison.OrdinalIgnoreCase)?"Ferien":s.Status;
        TextAt(g,shortStatus,22,Yellow,new(270,49,226,29),true);
        if(s.Remaining!=null)TextAt(g,s.Current?.Start==675?"Mensazeit · "+s.Range:s.Range,12,Color.Silver,new(270,83,226,21));
        var bg=dark?Color.FromArgb(43,41,25):Yellow;var fg=dark?Yellow:Ink;
        bool idle=s.Remaining==null;
        Box(g,bg,8,127,504,idle?66:98);
        if(idle)
        {
            string greeting=s.Status=="Unterrichtsfreie Zeit"?"Schönen Feierabend!":s.Status=="Wochenende"?"Schönes Wochenende!":s.Status.Contains("ferien",StringComparison.OrdinalIgnoreCase)?"Schöne Ferien!":"Genieße deinen freien Tag!";
            TextAt(g,greeting,22,fg,new(20,135,396,30),true);
            TextAt(g,s.Next,13,dark?Color.FromArgb(229,219,155):Ink,new(20,168,478,18));
            g.Restore(saved);return;
        }
        TextAt(g,s.Label.ToUpperInvariant(),10,fg,new(20,134,385,14),true);
        string min=s.Remaining is int sec?(sec/60).ToString("00"):"--",secs=s.Remaining is int sec2?(sec2%60).ToString("00"):"--";
        TextAt(g,min,33,fg,new(18,146,77,40),true);TextAt(g,"Minuten",12,fg,new(97,158,54,22));TextAt(g,secs,33,fg,new(158,146,63,40),true);TextAt(g,"Sekunden",12,fg,new(227,158,70,22));
        for(int i=0;i<20;i++){float bx=20+i*24;var c=(i+1)/20d<=s.Progress?fg:(dark?Color.FromArgb(75,68,18):Color.FromArgb(213,187,0));Box(g,c,bx,190,21,7,1);}
        TextAt(g,s.Next,12,dark?Color.FromArgb(229,219,155):Ink,new(20,200,478,18));
        g.Restore(saved);
    }
    protected override void Dispose(bool disposing)
    {
        if(disposing){SystemEvents.UserPreferenceChanged-=SystemPreferenceChanged;tick.Stop();sync.Stop();tick.Dispose();sync.Dispose();webTip.Dispose();lifetime.Cancel();logo.Dispose();logoDark.Dispose();}
        base.Dispose(disposing);
    }
}
internal sealed class Canvas:Control
{
    public Canvas(){DoubleBuffered=true;ResizeRedraw=true;AccessibleName="Unterrichtszeiten";}
}
internal sealed class PolishedRenderer:ToolStripProfessionalRenderer
{
    public PolishedRenderer(){RoundedEdges=false;}
    private static bool Dark(ToolStripItem item)=>item.Owner?.BackColor.GetBrightness()<.4f;
    protected override void OnRenderToolStripBorder(ToolStripRenderEventArgs e) { }
    protected override void OnRenderButtonBackground(ToolStripItemRenderEventArgs e)
    {
        bool check=e.Item is ToolStripButton button&&button.Checked;
        if(!e.Item.Selected&&!e.Item.Pressed&&!check)return;
        var color=e.Item.Name=="close"?Color.FromArgb(194,45,48):check?Color.FromArgb(245,214,0):Dark(e.Item)?Color.FromArgb(64,69,65):Color.FromArgb(224,226,220);
        using var brush=new SolidBrush(color);e.Graphics.FillRectangle(brush,new Rectangle(1,2,e.Item.Width-2,e.Item.Height-4));
    }
    protected override void OnRenderItemText(ToolStripItemTextRenderEventArgs e)
    {
        if(e.Item is ToolStripButton button&&button.Checked)e.TextColor=Color.FromArgb(32,35,34);
        else if(e.Item.Name=="close"&&(e.Item.Selected||e.Item.Pressed))e.TextColor=Color.White;
        base.OnRenderItemText(e);
    }
}
