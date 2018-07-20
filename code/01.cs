//#define Delta_Tool //Delta_Tool模式 at 2017/08/21
//#define RunDebug //執行偵錯瑪 at 2017/09/14
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using System.Collections;
using System.IO;
using MySql.Data.MySqlClient;//using Finisar.SQLite;
using Newtonsoft.Json;
using System.Threading;
using System.Diagnostics;
using System.Reflection;
using System.IO.Ports;//使用外部函式庫- Newtonsoft.Json.dll (VS2010- .net4.0 )

namespace SYWEB_V8_Workstation
{
    public partial class Main_Frm : Form
    {
        //---
        //Outlook子按鈕點擊後，保持顏色識別
        public int m_intOutlookClickMainIndex = -1;
        public int m_intOutlookClickSubIndex = -1;
        //---Outlook子按鈕點擊後，保持顏色識別

        private Image m_Img_g = Image.FromFile(System.Windows.Forms.Application.StartupPath + "\\images\\gball.png");//add at 2017/08/07
        private Image m_Img_r = Image.FromFile(System.Windows.Forms.Application.StartupPath + "\\images\\rball.png");//add at 2017/08/07
        private Image m_Img_n = Image.FromFile(System.Windows.Forms.Application.StartupPath + "\\images\\nball.png");//add at 2017/08/07

        //---
        //讀取AssemblyInfo.cs內的版本資訊
        private static System.Reflection.Assembly assembly = System.Reflection.Assembly.GetExecutingAssembly();
        private static FileVersionInfo fvi = FileVersionInfo.GetVersionInfo(assembly.Location);

        private static Assembly assem = Assembly.GetEntryAssembly();
        private static AssemblyName assemName = assem.GetName();
        private static Version ver = assemName.Version;

        private static string version = ver.ToString().Replace(".", "");//程式標題改成『Workstation [for SYDM]- V版號』 - private static string version = ver.ToString().Replace(".", "") + " [" + fvi.FileVersion + "]";
        private static string productVersion = "- V" + version;
        //---讀取AssemblyInfo.cs內的版本資訊

        public bool m_blnAPI;//SYDM和SYCG API呼叫並存實現

        public bool m_changeSYCGMode = true;//新增SYCG模式切換模式
        public bool m_changeToolMode = false;//切換模式 2017/05/18 09:15
        public bool m_blnLoad;
        public Settings m_Settings;
        private TabPage m_TPOld;//--2017/02/22 製作返回按鈕功能 //紀錄上一次的頁面
        private Stack m_StackTPOld = new Stack();//--2017/02/22 製作返回按鈕功能 //後進先出
        private OutlookBar m_OutlookBar1;//menu_step01
        static public Main_Frm pForm1;
        private readonly DisplaySettings _originalSettings;//恢復預設解析度-2017/02/03
        public CS_PHP m_CS_PHP;

        //---
        //紀錄編輯控制器三個表的uid at 2017/07/28
        public int m_intcontroller_sn;
        public static ArrayList m_ALDoors = new ArrayList();
        public static ArrayList m_ALDoor_id = new ArrayList();
        //---紀錄編輯控制器三個表的uid at 2017/07/28

        //---
        //遠端DB相關變數
        public External_MySQL m_ExMySQL = new External_MySQL();//增加連接SERVER DB元件
        public String m_StrDumpWherecondition;//下載指定時間報表資料 
        //---遠端DB相關變數

        public String m_StrAPBSydmid = "0";//SYCG/SYDM模式下SYDM ID綁定程式

        public int m_intSYDM_id;//SYCG模式下-建立/暫存 當下要操作的SYDM ID
        
        public Main_Frm()
        {
            #if (RunDebug)
                Stopwatch stopWatch = new Stopwatch();
                stopWatch.Start();
            #endif

            m_intSYDM_id = -1;//SYCG模式下-建立/暫存 當下要操作的SYDM ID

            m_Settings = new Settings();
            this.Font = new Font(m_Settings.m_StrFontName, Int32.Parse(m_Settings.m_StrFontSize));//2017/03/01
            m_blnLoad = false;
            //--
            if (m_Settings.m_StrAutoDisplay == "True")
            {
                //--
                //調整解析度-2017/02/03
                _originalSettings = DisplayManager.GetCurrentSettings();//恢復預設解析度-2017/02/03
                Display_API m_Display_API;
                m_Display_API = new Display_API();
                //Display_API.DEVMODE d1 = m_Display_API.CallEnumDisplaySettings();
                //AutoSize_DisplaySetting.m_intOldFrequency = d1.dmDisplayFrequency;
                m_Display_API.getDisplaySetting();
                AutoSize_DisplaySetting.m_intOldWidth = -1;
                AutoSize_DisplaySetting.m_intOldHeight = -1;
                //MessageBox.Show("get_Form1()-" + m_Display_API.m_intWidth + " X " + m_Display_API.m_intHeight);
                if ((m_Display_API.m_intWidth > 1920) && (m_Display_API.m_intHeight > 1080))
                {
                    AutoSize_DisplaySetting.m_intOldWidth = m_Display_API.m_intWidth;
                    AutoSize_DisplaySetting.m_intOldHeight = m_Display_API.m_intHeight;
                    AutoSize_DisplaySetting.changeResolution(1920, 1080, 60);
                }
                //--
            }

            InitializeComponent();
            m_tabMain.DrawMode = System.Windows.Forms.TabDrawMode.OwnerDrawFixed;//增加可改變頁籤顏色功能
            Form.CheckForIllegalCrossThreadCalls = false;//設定C# 跨執行緒(thread)存取UI
            pForm1 = this;
            m_OutlookBar1 = new OutlookBar();//menu_step02
            m_OutlookBar1.AutoScroll = false;//true;
            m_OutlookBar1.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
            m_OutlookBar1.ButtonHeight = 55;//修改左側主選單的高度，為了增加ICON做準備 35
            m_OutlookBar1.Dock = System.Windows.Forms.DockStyle.Fill;
            m_OutlookBar1.Location = new System.Drawing.Point(0, 0);
            m_OutlookBar1.Name = "outlookBar1";
            m_OutlookBar1.SelectedBand = 2;
            m_OutlookBar1.Size = new System.Drawing.Size(254, 259);
            m_OutlookBar1.TabIndex = 0;
            this.splitContainer1.Panel1.Controls.Add(m_OutlookBar1);

            if (m_Settings.m_StrAutoDisplay == "True")
            {
                //--
                //2017/01/13 針對系統解析度+放大倍率 做的自動調整設定
                /*
                 表單的屬性要做的對應設定
                 this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.None;
                */
                if (AutoSize_DisplaySetting.m_intOldWidth == -1 && AutoSize_DisplaySetting.m_fltDFactor_H == -1)//調整解析度-2017/02/03
                {
                    AutoSize_DisplaySetting.CalculateVar(this);
                    if (AutoSize_DisplaySetting.m_fltSysFactor_H > 1.6)
                    {
                        // 2017/01/29 防止字型大小比1還小
                        this.Font = new Font(this.Font.Name, this.Font.Size * 0.64f, this.Font.Style, this.Font.Unit);//所有主頁調整
                    }
                    else
                    {
                        this.Font = new Font(this.Font.Name, this.Font.Size * (1 / AutoSize_DisplaySetting.m_fltSysFactor_H), this.Font.Style, this.Font.Unit);//所有主頁調整
                    }
                }
                //所有實際頁面的個別調整
                //AutoSize_DisplaySetting.setTag(m_tabSub0000);
                //AutoSize_DisplaySetting.setControls_Position_Size((1 / AutoSize_DisplaySetting.m_fltSysFactor_W), (1 / AutoSize_DisplaySetting.m_fltSysFactor_H), m_tabSub0000);

                //AutoSize_DisplaySetting.setTag(m_tabSub000001);
                //AutoSize_DisplaySetting.setControls_Position_Size((1 / AutoSize_DisplaySetting.m_fltSysFactor_W), (1 / AutoSize_DisplaySetting.m_fltSysFactor_H), m_tabSub000001);

                //--
            }
            hideTabPage();

            #if (RunDebug)
                stopWatch.Stop();
                FileLib.logFile("log.txt", "Form1()-" + stopWatch.Elapsed.TotalMilliseconds.ToString());
            #endif

        }

        public ArrayList GetDistinctArray(ArrayList arr)//ArrayList 重複資料刪除
        {
            ArrayList lst = new ArrayList();
            for (int i = 0; i < arr.Count; i++)
            {
                if (lst.Contains(arr[i]))
                {
                    continue;
                }
                lst.Add(arr[i]);
            }
            return lst;           
        }

        public bool IsDistinctALObj(ArrayList arr)//判斷 ArrayList內元素是否都為不重複元素
        {
            bool blnAns = true;
            ArrayList lst = new ArrayList();
            for (int i = 0; i < arr.Count; i++)
            {
                if (lst.Contains(arr[i]))
                {
                    blnAns = false;
                    break;
                }
                else
                {
                    lst.Add(arr[i]);
                }
            }
            return blnAns;
        }

        public bool CheckDBObjectNotRepeat(ArrayList AL, int model)//mdel 0->door,1->card
        {
            bool blnAns = true;

            String SQL = "";
            String StrInputBuf = "";
            String StrOPBuf = "";

            ArrayList ALOP = new ArrayList();
            ALOP.Clear();

            for (int i = 0; i < AL.Count; i++)
            {
                StrInputBuf = AL[i].ToString();
                StrOPBuf = StrInputBuf.Substring(0, StrInputBuf.IndexOf(','));
                if (StrInputBuf.Contains(",-1") == true)
                {
                    switch (model)
                    {
                        case 0://door
                            SQL = String.Format("SELECT door_id AS data FROM door_group_detail WHERE door_group_id={0};", StrOPBuf);
                            break;
                        case 1://card
                            SQL = String.Format("SELECT card_id AS data FROM user_car_group_detailed WHERE user_car_group_id={0};", StrOPBuf);
                            break;
                    }
                    MySqlDataReader Reader_Data = MySQL.GetDataReader(SQL);
                    while (Reader_Data.Read())
                    {
                        ALOP.Add(Reader_Data["data"].ToString());
                    }
                    Reader_Data.Close();
                }
                else
                {
                    ALOP.Add(StrOPBuf);
                }
            }

            blnAns = IsDistinctALObj(ALOP);

            return blnAns;
        }

        public bool CheckUIVarNotChange(ArrayList ALInit, ArrayList ALData)
        {
            bool blnAns = true;

            if (ALInit.Count == ALData.Count)
            {
                for (int i = 0; i < ALInit.Count; i++)
                {
                    if ( ALInit[i].ToString() != ALData[i].ToString() )
                    {
                        blnAns = false;
                        break;
                    }
                }
            }
            else
            {
                blnAns = false;
            }

            return blnAns;
        }

        public void initTabPage()
        {
            m_tabPMainWel.Text = Language.m_StrTabPageTagWel;
            m_tabPMain00.Text = Language.m_StrTabPageTag00;
            m_tabPMain01.Text = Language.m_StrTabPageTag01;
            m_tabPMain02.Text = Language.m_StrTabPageTag02;
            m_tabPMain03.Text = Language.m_StrTabPageTag03;
            m_tabPMain04.Text = Language.m_StrTabPageTag04;
            m_tabSub0000.Text = Language.m_StrTabPageTag0000;
            m_tabSub000001.Text = Language.m_StrTabPageTag000001;
            m_tabSub0001.Text = Language.m_StrTabPageTag0001;
            m_tabSub000100.Text = Language.m_StrTabPageTag000100;
            m_tabSub000101.Text = Language.m_StrTabPageTag000101;
            m_tabSub0002.Text = Language.m_StrTabPageTag0002;
            m_tabSub000200.Text = Language.m_StrTabPageTag000200;
            m_tabSub0003.Text = Language.m_StrTabPageTag0003;
            m_tabSub000301.Text = Language.m_StrTabPageTag000301;
            //--
            //開發SYDM UI-系統載入時『列表m_tabSub0004』和『編輯m_tabSub000400』元件基本初始化
            m_tabSub0004.Text = Language.m_StrTabPageTag0004;
            m_tabSub000400.Text = Language.m_StrTabPageTag000400;
            //--
            //--
            //開發報表 UI-系統預設『列表m_tabSub0300』元件基本初始化
            m_tabSub0300.Text = Language.m_StrTabPageTag0300;
            //--
            //--
            //開發建立指紋 UI-系統預設『列表m_tabSub0400』元件基本初始化
            m_tabSub0400.Text = Language.m_StrTabPageTag0400;
            //--
            m_tabSub0100.Text = Language.m_StrlabSub0100;
            m_tabSub0101.Text = Language.m_StrlabSub0101;
            m_tabSub0102.Text = Language.m_StrlabSub0102;
            m_tabSub0103.Text = Language.m_StrlabSub0103;
            m_tabSub0104.Text = Language.m_StrlabSub0104;
            m_tabSub0200.Text = Language.m_StrlabSub0200;
            m_tabSub010000.Text = Language.m_StrlabSub010000;
            m_tabSub010100.Text = Language.m_StrlabSub010100;
            m_tabSub010200.Text = Language.m_StrlabSub010200;
            m_tabSub010400.Text = Language.m_StrlabSub0104;
            m_tabSub020000.Text = Language.m_StrlabSub020000;
            m_tabSub0201.Text = Language.m_StrlabSub0201;
            m_tabSub0202.Text = Language.m_StrlabSub0202;
            m_tabSub0203.Text = Language.m_StrlabSub0203;
            m_tabSub020300.Text = Language.m_StrlabSub020300;
            m_tabSys.Text = Language.m_StrTabPageTagSys;//2017/02/24 製作系統設定頁面

            m_tabMain.ShowToolTips = true;//模仿GOOGLE頁籤沒有顯示完整Title時，會用Tip來彌補

        }
        public void CreateMenu()
        {
            #if (RunDebug)
                Stopwatch stopWatch = new Stopwatch();
                stopWatch.Start();
            #endif

            if (m_OutlookBar1 != null)
            {
                IconPanel iconPanel0 = new IconPanel();
                IconPanel iconPanel1 = new IconPanel();
                IconPanel iconPanel2 = new IconPanel();
                IconPanel iconPanel3 = new IconPanel();
                IconPanel iconPanel4 = new IconPanel();
                IconPanel iconPanel5 = new IconPanel();//20170220--為了實作系統統設定UI

                m_OutlookBar1.DelAllBand();

                m_OutlookBar1.AddBand(imltOutlookMain_G.Images[1], imltOutlookMain_C.Images[1], Language.m_StrOutlookMainMenu00, iconPanel0, 0); //Outlook主按鈕加上圖片切換功能 //m_OutlookBar1.AddBand(Language.m_StrOutlookMainMenu00, iconPanel0, 0);//裝置管理
                if (!m_changeToolMode)
                {
                    //*對應 隱藏分頁-2017/02/17 把主選單隱藏起來-2017/02/17
                    m_OutlookBar1.AddBand(imltOutlookMain_G.Images[3], imltOutlookMain_C.Images[3], Language.m_StrOutlookMainMenu01, iconPanel1, 1);//Outlook主按鈕加上圖片切換功能 //m_OutlookBar1.AddBand(Language.m_StrOutlookMainMenu01, iconPanel1,1);//人員卡片管理
                    m_OutlookBar1.AddBand(imltOutlookMain_G.Images[5], imltOutlookMain_C.Images[5], Language.m_StrOutlookMainMenu02, iconPanel2, 2);//Outlook主按鈕加上圖片切換功能 //m_OutlookBar1.AddBand(Language.m_StrOutlookMainMenu02, iconPanel2,2);//門區通行授權

                    //---
                    //確認只有在SYCG模式才能有DB匯入匯出和報表功能+指紋功能
                    if (m_changeSYCGMode)
                    {
                        m_OutlookBar1.AddBand(imltOutlookMain_G.Images[6], imltOutlookMain_C.Images[6], Language.m_StrOutlookMainMenu03, iconPanel3, 3);//Outlook主按鈕加上圖片切換功能 //m_OutlookBar1.AddBand(Language.m_StrOutlookMainMenu03, iconPanel3, 3);//報表作業 //主功能選單增加報表選單
                        m_OutlookBar1.AddBand(imltOutlookMain_G.Images[0], imltOutlookMain_C.Images[0], Language.m_StrOutlookMainMenu04, iconPanel4, 4);//Outlook主按鈕加上圖片切換功能 //m_OutlookBar1.AddBand(Language.m_StrOutlookMainMenu04, iconPanel4, 4);//啟用主功能表-指紋管理
                    }
                    //---確認只有在SYCG模式才能有DB匯入匯出和報表功能+指紋功能


                    //*/
                }

                m_OutlookBar1.AddBand(imltOutlookMain_G.Images[7], imltOutlookMain_C.Images[7], Language.m_StrOutlookMainMenu05, iconPanel5, 5);//Outlook主按鈕加上圖片切換功能 //2017/02/24 製作系統設定頁面//m_OutlookBar1.AddBand(Language.m_StrOutlookMainMenu05, iconPanel5,5);//2017/02/24 製作系統設定頁面


                iconPanel0.AddIcon(Language.m_StrOutlookSubMenu00, imageList1.Images[16], new EventHandler(OutlookSubButton_Click), m_OutlookBar1.ButtonHeight / 10, 1, 0);//裝置管理-控制器
                iconPanel0.AddIcon(Language.m_StrOutlookSubMenu01, imageList1.Images[10], new EventHandler(OutlookSubButton_Click), m_OutlookBar1.ButtonHeight/10, 1,1);//裝置管理-門區
                if (!m_changeToolMode)//為了隱藏~區域門區群組 at 2017/08/01
                {
                    iconPanel0.AddIcon(Language.m_StrOutlookSubMenu02, imageList1.Images[15], new EventHandler(OutlookSubButton_Click), m_OutlookBar1.ButtonHeight / 10, 1, 2);//裝置管理-區域門區群組
                }
                iconPanel0.AddIcon(Language.m_StrOutlookSubMenu03, imageList1.Images[22], new EventHandler(OutlookSubButton_Click), m_OutlookBar1.ButtonHeight / 10, 1, 3);//裝置管理-門區A.P.B管理

                //---
                #if (!Delta_Tool)//修正隱藏UI功能把SYDM按鈕在切換至台達板時也要隱藏
                if (m_changeSYCGMode == true)//新增SYCG模式切換模式
                {
                    iconPanel0.AddIcon(Language.m_StrOutlookSubMenu04, imageList1.Images[53], new EventHandler(OutlookSubButton_Click), m_OutlookBar1.ButtonHeight / 10, 1, 4);//主功能選單增加SYDM
                }
                #endif
                //---
                
                if (!m_changeToolMode)
                {
                    //*對應 隱藏分頁-2017/02/17 把子選單隱藏起來-2017/02/17
                    iconPanel1.AddIcon(Language.m_StrOutlookSubMenu13, imageList1.Images[2], new EventHandler(OutlookSubButton_Click), m_OutlookBar1.ButtonHeight / 10, 1, 3);//人員卡片管理-部門管理 //調整人員OUTLOOK選單的排列位置=部、人、卡、車、群                 
                    iconPanel1.AddIcon(Language.m_StrOutlookSubMenu10, imageList1.Images[25], new EventHandler(OutlookSubButton_Click), m_OutlookBar1.ButtonHeight / 10, 1, 0);//人員卡片管理-人員資料管理 //調整人員OUTLOOK選單的排列位置=部、人、卡、車、群
                    iconPanel1.AddIcon(Language.m_StrOutlookSubMenu12, imageList1.Images[17], new EventHandler(OutlookSubButton_Click), m_OutlookBar1.ButtonHeight / 10, 1, 2);//人員卡片管理-卡片資料管理
                    iconPanel1.AddIcon(Language.m_StrOutlookSubMenu11, imageList1.Images[5], new EventHandler(OutlookSubButton_Click), m_OutlookBar1.ButtonHeight / 10, 1, 1);//人員卡片管理-車輛資料管理                 
                    iconPanel1.AddIcon(Language.m_StrOutlookSubMenu14, imageList1.Images[52], new EventHandler(OutlookSubButton_Click), m_OutlookBar1.ButtonHeight / 10, 1, 4);//人員卡片管理-人員車輛群組管理

                    iconPanel2.AddIcon(Language.m_StrOutlookSubMenu20, imageList1.Images[19], new EventHandler(OutlookSubButton_Click), m_OutlookBar1.ButtonHeight / 10, 1, 0);//門區通行授權-人員門區通行授權
                    iconPanel2.AddIcon(Language.m_StrOutlookSubMenu23, imageList1.Images[33], new EventHandler(OutlookSubButton_Click), m_OutlookBar1.ButtonHeight / 10, 1, 3);//add 2017/11/03 門區通行授權-授權查詢

                    iconPanel3.AddIcon(Language.m_StrOutlookSubMenu30, imageList1.Images[54], new EventHandler(OutlookSubButton_Click), m_OutlookBar1.ButtonHeight / 10, 3, 0);///主功能選單增加報表選單
                    /*
                    //還未開發所以先隱藏- at 2017/10/24
                    iconPanel2.AddIcon(Language.m_StrOutlookSubMenu22, imageList1.Images[50], new EventHandler(OutlookSubButton_Click), m_OutlookBar1.ButtonHeight / 10, 1, 2);//add 2017/11/03 門區通行授權-門區授權複製
                    iconPanel2.AddIcon(Language.m_StrOutlookSubMenu21, imageList1.Images[49], new EventHandler(OutlookSubButton_Click), m_OutlookBar1.ButtonHeight / 10, 1, 1);//add 2017/11/03 門區通行授權-人員授權複製
                    //*/
                    //*/ 
                }

                //---
                //啟用子功能表-指紋管理
                iconPanel4.AddIcon(Language.m_StrOutlookSubMenu40, imageList1.Images[55], new EventHandler(OutlookSubButton_Click), m_OutlookBar1.ButtonHeight / 10, 4, 0);//2017/02/24 製作系統設定頁面
                iconPanel4.AddIcon(Language.m_StrOutlookSubMenu41, imageList1.Images[56], new EventHandler(OutlookSubButton_Click), m_OutlookBar1.ButtonHeight / 10, 4, 1);//2017/02/24 製作系統設定頁面
                //---啟用子功能表-指紋管理

                iconPanel5.AddIcon(Language.m_StrTabPageTagSys, imageList1.Images[42], new EventHandler(OutlookSubButton_Click), m_OutlookBar1.ButtonHeight / 10, 1, 0);//2017/02/24 製作系統設定頁面
            }

            #if (RunDebug)
                stopWatch.Stop();
                FileLib.logFile("log.txt", "CreateMenu()-" + stopWatch.Elapsed.TotalMilliseconds.ToString());
            #endif
        }

        public void hideTabPage()
        {

            #if (RunDebug)
                Stopwatch stopWatch = new Stopwatch();
                stopWatch.Start();
            #endif

            //--
            //隱藏分頁-2017/02/17
            this.m_tabPMainWel.Parent = null; // hide
            this.m_tabPMain00.Parent = null; // hide
            this.m_tabPMain01.Parent = null; // hide
            this.m_tabPMain02.Parent = null; // hide
            this.m_tabPMain03.Parent = null; // hide
            this.m_tabPMain04.Parent = null; // hide
            //--
            //--
            //隱藏預備用分頁-2017/02/23
            /*
            this.m_tabtemp01.Parent = null; // hide
            this.m_tabtemp02.Parent = null; // hide
            this.m_tabtemp03.Parent = null; // hide
            this.m_tabtemp04.Parent = null; // hide
            this.m_tabtemp05.Parent = null; // hide
            this.m_tabtemp06.Parent = null; // hide
            this.m_tabtemp07.Parent = null; // hide
            this.m_tabtemp08.Parent = null; // hide
            this.m_tabtemp09.Parent = null; // hide
            this.m_tabtemp10.Parent = null; // hide
            this.m_tabtemp11.Parent = null; // hide
            this.m_tabtemp12.Parent = null; // hide
            this.m_tabtemp13.Parent = null; // hide
            this.m_tabtemp14.Parent = null; // hide
            this.m_tabtemp15.Parent = null; // hide
            this.m_tabtemp16.Parent = null; // hide
            this.m_tabtemp17.Parent = null; // hide
            this.m_tabtemp18.Parent = null; // hide
            this.m_tabtemp19.Parent = null; // hide
            this.m_tabtemp20.Parent = null; // hide
            this.m_tabtemp21.Parent = null; // hide
            this.m_tabtemp22.Parent = null; // hide
            this.m_tabtemp23.Parent = null; // hide
            this.m_tabtemp24.Parent = null; // hide
            this.m_tabtemp25.Parent = null; // hide
            this.m_tabtemp26.Parent = null; // hide
            this.m_tabtemp27.Parent = null; // hide
            this.m_tabtemp28.Parent = null; // hide
            this.m_tabtemp29.Parent = null; // hide
            this.m_tabtemp30.Parent = null; // hide
            this.m_tabtemp31.Parent = null; // hide
            this.m_tabtemp32.Parent = null; // hide
            this.m_tabtemp33.Parent = null; // hide
            this.m_tabtemp34.Parent = null; // hide
            this.m_tabtemp35.Parent = null; // hide
            this.m_tabtemp36.Parent = null; // hide
            this.m_tabtemp37.Parent = null; // hide
            this.m_tabtemp38.Parent = null; // hide
            this.m_tabtemp39.Parent = null; // hide
            this.m_tabtemp40.Parent = null; // hide
            this.m_tabtemp41.Parent = null; // hide
            this.m_tabtemp42.Parent = null; // hide
            this.m_tabtemp43.Parent = null; // hide
            this.m_tabtemp44.Parent = null; // hide
            this.m_tabtemp45.Parent = null; // hide
            this.m_tabtemp46.Parent = null; // hide
            this.m_tabtemp47.Parent = null; // hide
            this.m_tabtemp48.Parent = null; // hide
            this.m_tabtemp49.Parent = null; // hide
            this.m_tabtemp50.Parent = null; // hide
            */
            //--

            //--
            //隱藏系統一開始時，沒用過的分頁-2017/03/02
            this.m_tabSub0000.Parent = null; // hide
            this.m_tabSub000001.Parent = null; // hide
            this.m_tabSub0003.Parent = null; // hide
            this.m_tabSub000301.Parent = null; // hide
            this.m_tabSub0001.Parent = null; // hide
            this.m_tabSub000100.Parent = null; // hide
            this.m_tabSub000101.Parent = null; // hide
            this.m_tabSub0002.Parent = null; // hide
            this.m_tabSub000200.Parent = null; // hide
            //--

            //--
            //隱藏人員卡片管理相關子頁-2017/03/06
            this.m_tabSub0100.Parent = null; // hide
            this.m_tabSub010000.Parent = null; // hide
            this.m_tabSub0101.Parent = null; // hide
            this.m_tabSub010100.Parent = null; // hide
            this.m_tabSub0102.Parent = null; // hide
            this.m_tabSub010200.Parent = null; // hide
            this.m_tabSub0103.Parent = null; // hide
            this.m_tabSub0104.Parent = null; // hide
            this.m_tabSub010400.Parent = null; // hide
            this.m_tabSub0200.Parent = null; // hide
            this.m_tabSub020000.Parent = null; // hide
            this.m_tabSub0201.Parent = null; // hide
            /*
                ckbSub020001_01.Checked = true;
                ckbSub020001_02.Checked = true;
                ckbSub020001_03.Checked = true;
                ckbSub020001_04.Checked = true;
                ckbSub020001_05.Checked = true;
                ckbSub020001_06.Checked = true;
                ckbSub020001_07.Checked = true;
                ckbSub020001_08.Checked = true;
                ckbSub020001_09.Checked = true;
                ckbSub020001_10.Checked = true;
                ckbSub020001_11.Checked = true;
                ckbSub020001_12.Checked = true;
                ckbSub020001_13.Checked = true;
                ckbSub020001_14.Checked = true;
                ckbSub020001_15.Checked = true;
                ckbSub020001_16.Checked = true;
                rdbSub020001_01.Checked = true;
                rdbSub020001_02.Checked = true;
                rdbSub020001_03.Checked = true;
                rdbSub020001_04.Checked = true;
                rdbSub020001_05.Checked = true;
                rdbSub020001_06.Checked = true;
                rdbSub020001_07.Checked = true;
                rdbSub020001_08.Checked = true;
                rdbSub020001_09.Checked = true;
                rdbSub020001_10.Checked = true;
                rdbSub020001_11.Checked = true;
                rdbSub020001_12.Checked = true;
            //*/
            this.m_tabSub0202.Parent = null; // hide
            this.m_tabSub0203.Parent = null; // hide
            this.m_tabSub020300.Parent = null; // hide at 2017/11/13
            //--

            //---
            //開發SYDM UI-系統載入時『列表m_tabSub0004』和『編輯m_tabSub000400』元件基本初始化~隱藏分頁
            this.m_tabSub0004.Parent = null; // hide
            this.m_tabSub000400.Parent = null; // hide
            //---
            //---
            //開發報表 UI-系統預設『列表m_tabSub0300』元件基本初始化
            this.m_tabSub0300.Parent = null; // hide
            //---

            this.m_tabSub0400.Parent = null; // hide

            #if (RunDebug)
                stopWatch.Stop();
                FileLib.logFile("log.txt", "hideTabPage()-" + stopWatch.Elapsed.TotalMilliseconds.ToString());
            #endif

        }

        private void Form1_Load(object sender, EventArgs e)
        {
            #if (RunDebug)
                Stopwatch stopWatch = new Stopwatch();
                stopWatch.Start();
            #endif
            #if (Delta_Tool)//Delta_Tool模式 at 2017/08/21
                panel7.Visible = false;
                gpbSub000100_10.Visible = false;
                gpbSub000100_09.Visible = false;
                gpbSub000100_08.Visible = false;
                gpbSub000100_02.Visible = false;
                panel17.Visible = false;
                gpbSub000100_02.Visible = false;
                //-----------------------------
                panel18.Location = new Point(4, 23);//原本=4,138
                gpbSub000100_01.Height = 70;//原本=179
                //-----------------------------
                gpbSub000100_04.Location = new Point(6, 107);//6, 468->6, 107
                //-----------------------------
                gpbSub000100_05.Location = new Point(6, 240);//6, 601->6, 240
                //-----------------------------
                panel13.Location = new Point(0, 26);//0, 302->0, 26
                gpbSub000100_11.Height = 250;//原本=517
                //-----------------------------
                gpbSub000100_12.Location = new Point(976, 290);//976, 553->976, 290
                //-----------------------------
                //此區塊為了讓UI靠左集中
                gpbSub000100_06.Location = new Point(6, 240 + 160 + 0);
                gpbSub000100_07.Location = new Point(6, 240 + 160 + 103 + 6);
                gpbSub000100_11.Location = new Point(489, 38);
                gpbSub000100_12.Location = new Point(489, 290);
                gpbSub000100_13.Location = new Point(6, 240 + 160 + 103 + 12 + 101);
            #endif

            Language.initVar();

            if (!m_changeToolMode)
            {
                if (m_changeSYCGMode)
                {
                    this.Text = "Workstation [for SYCG]" + productVersion;//編譯SYCG/SYDM不同版本時抬頭要跟著改變 //程式標題改成『Workstation [for SYDM]- V版號』
                }
                else
                {
                    this.Text = "Workstation [for SYDM]" + productVersion;//編譯SYCG/SYDM不同版本時抬頭要跟著改變 //程式標題改成『Workstation [for SYDM]- V版號』
                }
            }
            else
            {
                if (m_changeSYCGMode)
                {
                    this.Text = "Tool [for SYCG]" + productVersion;//編譯SYCG/SYDM不同版本時抬頭要跟著改變 //程式標題改成『Workstation [for SYDM]- V版號』
                }
                else
                {
                    this.Text = "Tool [for SYDM]" + productVersion;//編譯SYCG/SYDM不同版本時抬頭要跟著改變 //程式標題改成『Workstation [for SYDM]- V版號』
                }
                #if (Delta_Tool)//Delta_Tool模式 at 2017/08/21
                    this.Text = "V8 Workstation_delta" + productVersion;
                #endif
            }

            if (MySQL.initMySQLDB())//SQLite.initSQLiteDatabase();//建立DB-2017/05/21
            {
                HW_Net_API.getHWInfo();//讀取機型到記憶體
                HW_Net_API.getCardType();//讀取卡片類型
                HW_Net_API.getRecordStatus();//撰寫把record_status.csv匯入資料庫中
                HW_Net_API.getfingerprint_type();//匯入fingerprint_type
                HW_Net_API.getsycg_command();//匯入sycg_command
                HW_Net_API.SYCG_setSYCGDomainURL();//製作SYDM列表UI-製作SYDM匯入功能
                HW_Net_API.getFD_model();//把指紋機型號寫入資料庫中
                HW_Net_API.getFD_type();//把指紋機種類寫入資料庫中
                HW_Net_API.getDepartmentItem();//建立預設部門
                
            }
            else
            {
                MessageBox.Show(Language.m_StrSysMsg00, butSub0000_01.Text, MessageBoxButtons.OK, MessageBoxIcon.Error);//MessageBox全部支援多國語系
                Application.Exit();
            }

            //USkinSDK.USkinLoadSkin("@WE Blue.msstyles");//2017/01/09-USkin
            /*
            //2017/01/09-USkin
            //http://blog.csdn.net/pipi0714/article/details/3979194
            启动程序，程序果然换肤了。但是最大化以后发现界面只显示正常状态大小的皮图其他的透明。
            将窗体的IsMdiContainer属性设置为true，解决问题
            */
            
            initTabPage();
            initWelcomeUI();
            initMain00UI();
            #if (!Delta_Tool)//修正隱藏UI功能把SYDM按鈕在切換至台達板時也要隱藏
	            JLMB_Main0004.Visible = true;
            #else
                JLMB_Main0004.Visible = false;
            #endif

            initMain01UI();
            initMain02UI();
            initMain03UI();
            initMain04UI();

            //---
            //啟動程式優化-停用所有子TabPage初始化 at 2018/05/07
            /*
            initSub0000UI();
            initSub000001UI();
            initSub0001UI();
            initSub000100UI();
            initSub000101UI();
            initSub0002UI();
            initSub000200UI();
            initSub0003UI();
            initSub000301UI();
            //---
            //開發SYDM UI-系統載入時『列表m_tabSub0004』和『編輯m_tabSub000400』元件基本初始化
            initSub0004UI();
            initSub000400UI();
            //---
            //---
            //開發報表 UI-系統預設『列表m_tabSub0300』元件基本初始化
            initSub0300UI();
            //---
            initSub0400UI();//抓取指紋UI初始化

            //--
            //add 2017/10/24
            if(!m_changeToolMode)
            {
                initSub0100UI();
                initSub0101UI();
                initSub0102UI();
                initSub0103UI();
                initSub0104UI();
                initSub0200UI();
                initSub010000UI();
                initSub010100UI();
                initSub010200UI();
                initSub010400UI();
                initSub020000UI();
                initSub0201UI();
                initSub0202UI();
                initSub0203UI();
                initSub020300UI();
            }
            //--
            */
            //---啟動程序優化-停用所有子TabPage初始化 at 2018/05/07

            initSysUI();
            PromptTextBox_initTipText();
            m_OutlookBar1.Initialize();//menu_step03
            CreateMenu();



            m_OutlookBar1.SelectedBand = m_OutlookBar1.intLastPage;//2017/10/31 //2017/01/08 預設開啟的功能選單
            m_tabMain.SelectedTab = m_tabSys;//2017/02/24 設定起始頁
            m_TPOld = m_tabMain.SelectedTab;//--2017/02/22 製作返回按鈕功能
            TabPage_Push();//m_StackTPOld.Push(m_TPOld);//--2017/02/22 製作返回按鈕功能

            m_blnLoad = true;

            m_CS_PHP = new CS_PHP();//建立WEB 連線元件


#if (RunDebug)
                stopWatch.Stop();
                FileLib.logFile("log.txt", "Form1_Load()-" + stopWatch.Elapsed.TotalMilliseconds.ToString());
#endif
            this.rptSub0300.RefreshReport();
        }
        public void OutlookSubButton_Click(object sender, EventArgs e)//Outlook Menu 子選單 「全部」 的事件反應區
        {
            Control ctrl = (Control)sender;
            PanelIcon panelIcon = ctrl.Tag as PanelIcon;
            //MessageBox.Show("#" + m_OutlookBar1.SelectedBand + "," + panelIcon.Index.ToString(), "Panel Event");
            //--
            //modified 2017/10/24
            /*
            if (!m_changeToolMode)
            {
                Change_tabMainSelectedTab((m_OutlookBar1.SelectedBand * 10 + panelIcon.Index));
            }
            else
            {
                if (m_OutlookBar1.SelectedBand == 0)
                {
                    if (panelIcon.Index <= 1)//為了隱藏~區域門區群組 at 2017/08/01
                    {
                        Change_tabMainSelectedTab((m_OutlookBar1.SelectedBand * 10 + panelIcon.Index));
                    }
                    else
                    {
                        Change_tabMainSelectedTab((m_OutlookBar1.SelectedBand * 10 + (panelIcon.Index + 1)));//為了隱藏~區域門區群組 at 2017/08/01
                    }
                }
                else
                {
                    Change_tabMainSelectedTab(((m_OutlookBar1.SelectedBand+4 )* 10 + panelIcon.Index));
                }
            }
            */
            m_OutlookBar1.SelectedBand = m_OutlookBar1.SelectedBand;//Outlook子按鈕點擊後，保持顏色識別
            Change_tabMainSelectedTab((m_OutlookBar1.SelectedBand * 10 + panelIcon.Index));
            
            //--
        }
        public void JLMultiLineButton(int Index)//所有JLMultiLineButton 的回應事件
        {
            m_OutlookBar1.SelectedBand = Index / 10;
            Change_tabMainSelectedTab(Index);
        }
        public void Change_tabMainSelectedTab(int Index)//JLMultiLineButton+Outlook Menu 子選單 實際頁面切換程式
        {
            m_TPOld = m_tabMain.SelectedTab;//--2017/02/22 製作返回按鈕功能
            TabPage_Push();//m_StackTPOld.Push(m_TPOld);//--2017/02/22 製作返回按鈕功能
            switch (Index)
            {
                case 0000:
                    m_tabSub0000.Parent = m_tabMain;//隱藏系統一開始時，沒用過的分頁，當要顯示時必須先指定父物件-2017/03/02
                    m_tabMain.SelectedTab = m_tabSub0000;
                    initSub0000UI();
                    //原本寫給邱總DEMO用的，現在把它停用 at 2017/06/26 //Animation.createThreadAnimation("載入控制器列表" + " ...", Animation.Thread_GetControllerList);//MessageBox.Show("get");
                    get_show_Controllers();
                    break;
                case 0001:
                    //*
                    m_tabSub0001.Parent = m_tabMain;//隱藏系統一開始時，沒用過的分頁，當要顯示時必須先指定父物件-2017/03/02
                    m_tabMain.SelectedTab = m_tabSub0001;//2017/02/08 add
                    initSub0001UI();
                    txtSub0001_01.Focus();//--2017/03/30 頁面切換後，指定該頁面特定元件取的焦點(Focus)
                    //*/
                    //MessageBox.Show("Unfulfilled");//先停用未完成的UI顯示 at 2017/07/03	
                    break;
                case 0002:
                    m_tabSub0002.Parent = m_tabMain;//隱藏系統一開始時，沒用過的分頁，當要顯示時必須先指定父物件-2017/03/02
                    initSub0002UI();
                    m_tabMain.SelectedTab = m_tabSub0002;//2017/02/15 add
                    butSub0002_01.Focus();//--2017/03/30 頁面切換後，指定該頁面特定元件取的焦點(Focus)
                    break;
                case 0003:
                    m_tabSub0003.Parent = m_tabMain;//隱藏系統一開始時，沒用過的分頁，當要顯示時必須先指定父物件-2017/03/02
                    initSub0003UI();
                    m_tabMain.SelectedTab = m_tabSub0003;
                    butSub0003_01.Focus();//--2017/03/30 頁面切換後，指定該頁面特定元件取的焦點(Focus)
                    break;
                case 0004:
                    //---
                    //開發SYDM UI-系統載入時『列表m_tabSub0004』和『編輯m_tabSub000400』元件基本初始化
                    m_tabSub0004.Parent = m_tabMain;//隱藏系統一開始時，沒用過的分頁，當要顯示時必須先指定父物件-2017/03/02
                    initSub0004UI();
                    m_tabMain.SelectedTab = m_tabSub0004;
                    //---
                    //MessageBox.Show(Language.m_StrOutlookSubMenu04);//主功能選單增加SYDM
                    break;
                case 0010:
                    m_tabSub0100.Parent = m_tabMain;//隱藏系統一開始時，沒用過的分頁，當要顯示時必須先指定父物件-2017/03/02
                    initSub0100UI(true);
                    m_tabMain.SelectedTab = m_tabSub0100;
                    break;
                case 0011:
                    m_tabSub0101.Parent = m_tabMain;//隱藏系統一開始時，沒用過的分頁，當要顯示時必須先指定父物件-2017/03/02
                    initSub0101UI(true);
                    m_tabMain.SelectedTab = m_tabSub0101;
                    break;
                case 0012:
                    m_tabSub0102.Parent = m_tabMain;//隱藏系統一開始時，沒用過的分頁，當要顯示時必須先指定父物件-2017/03/02
                    initSub0102UI(true);
                    m_tabMain.SelectedTab = m_tabSub0102;
                    break;
                case 0013:
                    m_tabSub0103.Parent = m_tabMain;//隱藏系統一開始時，沒用過的分頁，當要顯示時必須先指定父物件-2017/03/02
                    initSub0103UI();
                    m_tabMain.SelectedTab = m_tabSub0103;
                    break;
                case 0014:
                    m_tabSub0104.Parent = m_tabMain;//隱藏系統一開始時，沒用過的分頁，當要顯示時必須先指定父物件-2017/03/02
                    initSub0104UI();
                    m_tabMain.SelectedTab = m_tabSub0104;
                    break;
                case 0020:
                    m_tabSub0200.Parent = m_tabMain;//隱藏系統一開始時，沒用過的分頁，當要顯示時必須先指定父物件-2017/03/02
                    initSub0200UI();
                    m_tabMain.SelectedTab = m_tabSub0200;
                    break;
                case 0021:
                    m_tabSub0201.Parent = m_tabMain;//隱藏系統一開始時，沒用過的分頁，當要顯示時必須先指定父物件-2017/03/02
                    initSub0201UI();
                    m_tabMain.SelectedTab = m_tabSub0201;
                    break;
                case 0022:
                    m_tabSub0202.Parent = m_tabMain;//隱藏系統一開始時，沒用過的分頁，當要顯示時必須先指定父物件-2017/03/02
                    initSub0202UI();
                    m_tabMain.SelectedTab = m_tabSub0202;
                    break;
                case 0023://add 2017/11/03
                    m_tabSub0203.Parent = m_tabMain;//隱藏系統一開始時，沒用過的分頁，當要顯示時必須先指定父物件-2017/03/02
                    initSub0203UI();
                    m_tabMain.SelectedTab = m_tabSub0203;
                    break;
                case 0030:
                    //---
                    //開發報表 UI-系統預設『列表m_tabSub0300』元件基本初始化
                    m_tabSub0300.Parent = m_tabMain;
                    initSub0300UI();
                    m_tabMain.SelectedTab = m_tabSub0300;
                    //---
                    //MessageBox.Show("0030");//報表作業 //主功能選單增加報表選單
                    break;
                case 0040:
                    //---
                    //顯示抓取指紋UI
                    m_tabSub0400.Parent = m_tabMain;
                    initSub0400UI();
                    m_tabMain.SelectedTab = m_tabSub0400;
                    //---顯示抓取指紋UI
                    break;
                case 0041:
                    MessageBox.Show("傳指紋");
                    break;
                case 0050:
                    m_tabMain.SelectedTab = m_tabSys;
                    break;
            }
        }
        private void splitContainer1_Panel1_Resize(object sender, EventArgs e)//讓Outlook Menu會隨著UI調整而自動重繪
        {
            CreateMenu();
        }
        public void OutlookMenuMain_Click(object sender, EventArgs e)//2017/01/10 讓Outlook 主按鈕的事件呼叫UI的事件函數
        {
            int Index;
            //--
            //modified 2017/10/24
            /*
            if (!m_changeToolMode)
            {
                Index = ((BandButton)(sender)).bti.index;
            }
            else
            {
                Index = ((BandButton)(sender)).bti.index;
                if (Index > 0)
                    Index = 5;
            }
            */
            Index = ((BandButton)(sender)).bti.index;
            //--
            m_TPOld = m_tabMain.SelectedTab;//--2017/02/22 製作返回按鈕功能
            TabPage_Push();//m_StackTPOld.Push(m_TPOld);//--2017/02/22 製作返回按鈕功能
            //--
            //--把所有Outlook主按鈕的切換頁面功能全部設定在系統頁面
            /*
            switch (Index)
            {
                case 0:
                m_tabPMain00.Parent = m_tabMain;
                m_tabMain.SelectedTab=m_tabPMain00;//MessageBox.Show("Main_0", "Panel Event");
                break;
                case 1:
                m_tabPMain01.Parent = m_tabMain;
                m_tabMain.SelectedTab=m_tabPMain01;//MessageBox.Show("Main_1", "Panel Event");
                break;
                case 2:
                m_tabPMain02.Parent = m_tabMain;
                m_tabMain.SelectedTab=m_tabPMain02;//MessageBox.Show("Main_2", "Panel Event");
                break;
                case 3:
                m_tabPMain03.Parent = m_tabMain;
                m_tabMain.SelectedTab=m_tabPMain03;//MessageBox.Show("Main_3", "Panel Event");
                break;
                case 4:
                m_tabPMain04.Parent = m_tabMain;
                m_tabMain.SelectedTab=m_tabPMain04;//MessageBox.Show("Main_4", "Panel Event");
                break;
                case 5:
                m_tabSys.Parent = m_tabMain;
                m_tabMain.SelectedTab = m_tabSys;//MessageBox.Show("Main_4", "Panel Event");
                break;
                //
            }
            */
            //---

            if (m_OutlookBar1.SelectedBand != Index)
            {
                m_intOutlookClickMainIndex = Index;
                m_intOutlookClickSubIndex = -1;
            }

            m_tabSys.Parent = m_tabMain;
            m_tabMain.SelectedTab = m_tabSys;
            //---把所有Outlook主按鈕的切換頁面功能全部設定在系統頁面

        }

        private void m_tabMain_SelectedIndexChanged(object sender, EventArgs e)//工作區改變時也同時改變左側選單
        {
            m_TPOld = m_tabMain.SelectedTab;//--2017/02/22 製作返回按鈕功能
            TabPage_Push();//m_StackTPOld.Push(m_TPOld);//--2017/02/22 製作返回按鈕功能
            
            //---
            //把所有Outlook主按鈕的切換頁面功能全部設定在系統頁面
            /*
            if (m_tabMain.SelectedTab == m_tabPMain00)
            {
                m_OutlookBar1.SelectedBand = 0;
            }
            if (m_tabMain.SelectedTab == m_tabPMain01)
            {
                m_OutlookBar1.SelectedBand = 1;
            }
            if (m_tabMain.SelectedTab == m_tabPMain02)
            {
                m_OutlookBar1.SelectedBand = 2;
            }
            if (m_tabMain.SelectedTab == m_tabPMain03)
            {
                m_OutlookBar1.SelectedBand = 3;
            }
            if (m_tabMain.SelectedTab == m_tabPMain04)
            {
                m_OutlookBar1.SelectedBand = 4;
            }
            if (m_tabMain.SelectedTab == m_tabSys)
            {
                m_OutlookBar1.SelectedBand = 5;
            }
            */
            if (m_tabMain.SelectedTab == m_tabSys)
            {
                if (m_intOutlookClickMainIndex == 5)
                {
                    m_OutlookBar1.SelectedBand = 5;
                }
            }
            //---把所有Outlook主按鈕的切換頁面功能全部設定在系統頁面

            //---
            //Outlook子按鈕點擊後，保持顏色識別
            m_intOutlookClickMainIndex = -1;
            m_intOutlookClickSubIndex = -1;
            //---Outlook子按鈕點擊後，保持顏色識別

            //---
            //裝置管理子頁選擇連動Outlook選單切換
            if (m_tabMain.SelectedTab == m_tabSub0000)
            {
                m_intOutlookClickMainIndex = 0;
                m_intOutlookClickSubIndex = 0;
                m_OutlookBar1.SelectedBand = 0;
            }
            if (m_tabMain.SelectedTab == m_tabSub000001)
            {
                m_intOutlookClickMainIndex = 0;
                m_intOutlookClickSubIndex = 0;
                m_OutlookBar1.SelectedBand = 0;
            }
            if (m_tabMain.SelectedTab == m_tabSub0001)
            {
                m_intOutlookClickMainIndex = 0;
                m_intOutlookClickSubIndex = 1;
                m_OutlookBar1.SelectedBand = 0;
            }
            if (m_tabMain.SelectedTab == m_tabSub000100)
            {
                m_intOutlookClickMainIndex = 0;
                m_intOutlookClickSubIndex = 1;
                m_OutlookBar1.SelectedBand = 0;
            }
            if (m_tabMain.SelectedTab == m_tabSub0002)
            {
                m_intOutlookClickMainIndex = 0;
                m_intOutlookClickSubIndex = 2;
                m_OutlookBar1.SelectedBand = 0;
            }
            if (m_tabMain.SelectedTab == m_tabSub000200)
            {
                m_intOutlookClickMainIndex = 0;
                m_intOutlookClickSubIndex = 2;
                m_OutlookBar1.SelectedBand = 0;
            }
            if (m_tabMain.SelectedTab == m_tabSub0003)
            {
                m_intOutlookClickMainIndex = 0;
                m_intOutlookClickSubIndex = 3;
                m_OutlookBar1.SelectedBand = 0;
            }
            if (m_tabMain.SelectedTab == m_tabSub000301)
            {
                m_intOutlookClickMainIndex = 0;
                m_intOutlookClickSubIndex = 3;
                m_OutlookBar1.SelectedBand = 0;
            }
            if (m_tabMain.SelectedTab == m_tabSub0004)
            {
                m_intOutlookClickMainIndex = 0;
                m_intOutlookClickSubIndex = 4;
                m_OutlookBar1.SelectedBand = 0;
            }
            if (m_tabMain.SelectedTab == m_tabSub000400)
            {
                m_intOutlookClickMainIndex = 0;
                m_intOutlookClickSubIndex = 4;
                m_OutlookBar1.SelectedBand = 0;
            }
            //---裝置管理子頁選擇連動Outlook選單切換 

            //---
            //人員卡片管理子頁選擇連動Outlook選單切換 
            if (m_tabMain.SelectedTab == m_tabSub0100)
            {
                m_intOutlookClickMainIndex = 1;
                m_intOutlookClickSubIndex = 1;
                m_OutlookBar1.SelectedBand = 1;
            }
            if (m_tabMain.SelectedTab == m_tabSub010000)
            {
                m_intOutlookClickMainIndex = 1;
                m_intOutlookClickSubIndex = 1;
                m_OutlookBar1.SelectedBand = 1;
            }
            if (m_tabMain.SelectedTab == m_tabSub0101)
            {
                m_intOutlookClickMainIndex = 1;
                m_intOutlookClickSubIndex = 3;
                m_OutlookBar1.SelectedBand = 1;
            }
            if (m_tabMain.SelectedTab == m_tabSub010100)
            {
                m_intOutlookClickMainIndex = 1;
                m_intOutlookClickSubIndex = 3;
                m_OutlookBar1.SelectedBand = 1;
            }
            if (m_tabMain.SelectedTab == m_tabSub0102)
            {
                m_intOutlookClickMainIndex = 1;
                m_intOutlookClickSubIndex = 2;
                m_OutlookBar1.SelectedBand = 1;
            }
            if (m_tabMain.SelectedTab == m_tabSub010200)
            {
                m_intOutlookClickMainIndex = 1;
                m_intOutlookClickSubIndex = 2;
                m_OutlookBar1.SelectedBand = 1;
            }
            if (m_tabMain.SelectedTab == m_tabSub0103)
            {
                m_intOutlookClickMainIndex = 1;
                m_intOutlookClickSubIndex = 0;
                m_OutlookBar1.SelectedBand = 1;
            }
            if (m_tabMain.SelectedTab == m_tabSub0104)
            {
                m_intOutlookClickMainIndex = 1;
                m_intOutlookClickSubIndex = 4;
                m_OutlookBar1.SelectedBand = 1;
            }
            if (m_tabMain.SelectedTab == m_tabSub010400)
            {
                m_intOutlookClickMainIndex = 1;
                m_intOutlookClickSubIndex = 4;
                m_OutlookBar1.SelectedBand = 1;
            }
            //---人員卡片管理子頁選擇連動Outlook選單切換

            //---
            //門區通行授權子頁選擇連動Outlook選單切換
            if (m_tabMain.SelectedTab == m_tabSub0200)
            {
                m_intOutlookClickMainIndex = 2;
                m_intOutlookClickSubIndex = 0;
                m_OutlookBar1.SelectedBand = 2;
            }
            if (m_tabMain.SelectedTab == m_tabSub020000)
            {
                m_intOutlookClickMainIndex = 2;
                m_intOutlookClickSubIndex = 0;
                m_OutlookBar1.SelectedBand = 2;
            }
            if (m_tabMain.SelectedTab == m_tabSub0201)
            {
                m_intOutlookClickMainIndex = 2;
                //m_intOutlookClickSubIndex = 0;
                m_OutlookBar1.SelectedBand = 2;
            }
            if (m_tabMain.SelectedTab == m_tabSub0202)
            {
                m_intOutlookClickMainIndex = 2;
                //m_intOutlookClickSubIndex = 2;
                m_OutlookBar1.SelectedBand = 2;
            }
            if (m_tabMain.SelectedTab == m_tabSub0203)
            {
                m_intOutlookClickMainIndex = 2;
                m_intOutlookClickSubIndex = 1;
                m_OutlookBar1.SelectedBand = 2;
            }
            if (m_tabMain.SelectedTab == m_tabSub020300)
            {
                m_intOutlookClickMainIndex = 2;
                m_intOutlookClickSubIndex = 1;
                m_OutlookBar1.SelectedBand = 2;
            }
            //---門區通行授權子頁選擇連動Outlook選單切換  

            //---
            //報表作業子頁選擇連動Outlook選單切換 
            if (m_tabMain.SelectedTab == m_tabSub0300)
            {
                m_intOutlookClickMainIndex = 3;
                m_intOutlookClickSubIndex = 0;
                m_OutlookBar1.SelectedBand = 3;
            }
            //---報表作業子頁選擇連動Outlook選單切換

            //---
            //指紋管理子頁選擇連動Outlook選單切換 
            if (m_tabMain.SelectedTab == m_tabSub0400)
            {
                m_intOutlookClickMainIndex = 4;
                m_intOutlookClickSubIndex = 0;
                m_OutlookBar1.SelectedBand = 4;
            }
            //---指紋管理子頁選擇連動Outlook選單切換
            
        }

        private void Form1_FormClosing(object sender, FormClosingEventArgs e)
        {
            /*
            //調整解析度-2017/02/03
            if ((AutoSize_DisplaySetting.m_intOldWidth != -1) && (AutoSize_DisplaySetting.m_intOldHeight != -1))
            {
                //MessageBox.Show("change-FormClosing" + AutoSize_DisplaySetting.m_intOldWidth + " X " + AutoSize_DisplaySetting.m_intOldHeight);
                AutoSize_DisplaySetting.changeResolution(AutoSize_DisplaySetting.m_intOldWidth, AutoSize_DisplaySetting.m_intOldHeight, AutoSize_DisplaySetting.m_intOldFrequency);
            }
            */
            DisplayManager.SetDisplaySettings(_originalSettings);//恢復預設解析度-2017/02/03
            MySQL.stopMySQL();
        }
        private void PromptTextBox_initTipText()
        {
            txtSub0000_01.TipText = Language.m_StrtxtSub0000_01;
            txtSub000001_06.TipText = Language.m_StrtxtSub0000_01;
            txtSub0003_01.TipText = Language.m_StrtxtSub0000_01;
            txtSub000301_03.TipText = Language.m_StrtxtSub0000_01;
            txtSub000301_02.TipText = Language.m_StrtxtSub0000_01;
            txtSub000301_04.TipText = Language.m_StrtxtSub0000_01;
            txtSub0001_01.TipText = Language.m_StrtxtSub0000_01;
            txtSub0001_02.TipText = Language.m_StrtxtSub0000_01;
            txtSub0002_01.TipText = Language.m_StrtxtSub0000_01;
            txtSub000200_02.TipText = Language.m_StrtxtSub0000_01;
            txtSub000200_03.TipText = Language.m_StrtxtSub0000_01;
        }

        public void Leave_function()
        {
            while (m_StackTPOld.Count > 0)
            {
                m_TPOld = ((TabPage)m_StackTPOld.Pop());
                if (m_TPOld != null && m_TPOld.Parent != null)//新增一個判斷防止UI重複亂跳 at 2017/09/01 if (m_TPOld != null)
                {
                    if (m_tabMain.SelectedTab != m_TPOld)
                    {
                        //--
                        TabPage TPNow = m_tabMain.SelectedTab;//隱藏目前分頁，但是要分開寫，不可抓到就直接執行，必須先指定目前新分頁，否則系統會產生錯亂-2017/03/02

                        m_TPOld.Parent = m_tabMain;
                        m_tabMain.SelectedTab = m_TPOld;

                        TPNow.Parent = null;//隱藏目前分頁，但是要分開寫，不可抓到就直接執行，必須先指定目前新分頁，否則系統會產生錯亂-2017/03/02
                        //--
                        break;
                    }
                }
            }
        }
        private void butLeave_Click(object sender, EventArgs e)
        {
            Leave_function();
        }

        //----Sub000001_start
        public CAAD_Controller UI_DB2CAAD_Controller(int intsy_dm_Controller_id)//Set Controller A.P.B & A/B
        {
            String SQL = "";
            CAAD_Controller CAAD_data = new CAAD_Controller();
            CAAD_data.apb_and_ab_door = new CAAD_ApbAndAbDoor();
            CAAD_data.apb_and_ab_door.apb_level_list = new List<int>();
            CAAD_data.apb_and_ab_door.apb_reset_timestamp_list = new List<int>();
            CAAD_data.identifier = intsy_dm_Controller_id;
            if (ckbSub000001_02.Checked)
            {
                CAAD_data.apb_and_ab_door.ab_door_enabled = 1;//Strab_door_enabled = "1";
            }
            else
            {
                CAAD_data.apb_and_ab_door.ab_door_enabled = 0;//Strab_door_enabled = "0";
            }
            CAAD_data.apb_and_ab_door.ab_door_level = txtSub000001_19.Value;
            CAAD_data.apb_and_ab_door.ab_door_timeout_second = 30;//預設初始值
            CAAD_data.apb_and_ab_door.ab_door_reset_time_second = 60;//預設初始值
            int intdoor_number = 0;
            SQL = String.Format("SELECT ab_door_timeout_second,ab_door_reset_time_second,door_number FROM controller_extend WHERE controller_sn={0};", labSub000001_09.Text);
            MySqlDataReader Reader_Data = MySQL.GetDataReader(SQL);
            while (Reader_Data.Read())
            {
                int tmp1 = 0, tmp2 = 0;
                tmp1 = Convert.ToInt32(Reader_Data["ab_door_timeout_second"].ToString());
                tmp2 = Convert.ToInt32(Reader_Data["ab_door_reset_time_second"].ToString());
                intdoor_number = Convert.ToInt32(Reader_Data["door_number"].ToString());
                if ((tmp1 > 0) && (tmp2 > 0))
                {
                    CAAD_data.apb_and_ab_door.ab_door_timeout_second = tmp1;//從SQL拿
                    CAAD_data.apb_and_ab_door.ab_door_reset_time_second = tmp2;//從SQL拿
                }
                break;
            }
            Reader_Data.Close();

            if (ckbSub000001_01.Checked)
            {
                CAAD_data.apb_and_ab_door.apb_group = 0;//SQL  
                CAAD_data.apb_and_ab_door.apb_enabled = 1;
                SQL = String.Format("SELECT d_e.apb_group_id AS apb_group_id FROM door_extend AS d_e,door AS d,controller AS c WHERE (d_e.door_id=d.id) AND (d.controller_id=c.sn) AND c.sn={0};", labSub000001_09.Text);
                MySqlDataReader Reader_Data1 = MySQL.GetDataReader(SQL);
                while (Reader_Data1.Read())
                {
                    int tmp = Convert.ToInt32(Reader_Data1["apb_group_id"].ToString());
                    if (tmp > 0)
                    {
                        CAAD_data.apb_and_ab_door.apb_group = tmp;
                    }
                    break;
                }
                Reader_Data1.Close();
            }
            else
            {
                CAAD_data.apb_and_ab_door.apb_enabled = 0;
                CAAD_data.apb_and_ab_door.apb_group = 0;
            }
            CAAD_data.apb_and_ab_door.apb_mode = 0;
            if (rdbSub000001_03.Checked)
            {
                CAAD_data.apb_and_ab_door.apb_mode = 1;
            }
            else if (rdbSub000001_04.Checked)
            {
                CAAD_data.apb_and_ab_door.apb_mode = 2;
            }
            int[] apb_level_list = new int[intdoor_number];
            for (int i = 0; i < intdoor_number; i++)
            {
                apb_level_list[i] = 0;
            }
            SQL = String.Format("SELECT d.controller_door_index AS id,d_e.apb_level AS apb_level FROM door_extend AS d_e,door AS d,controller AS c WHERE (c.sn={0})AND(c.sn=d.controller_id)AND(d_e.door_id=d.id) ORDER BY d.controller_door_index;", labSub000001_09.Text);//修改 『d.controller_door_index AS id』 2017/08/04
            MySqlDataReader Reader_Data2 = MySQL.GetDataReader(SQL);
            while (Reader_Data2.Read())
            {
                int id = 0, value = 0;
                id = Convert.ToInt32(Reader_Data2["id"].ToString());
                value = Convert.ToInt32(Reader_Data2["apb_level"].ToString());
                apb_level_list[(id - 1)] = value;//id-1 原因 門從1開始，但陣列從0開始
            }
            Reader_Data2.Close();
            for (int i = 0; i < intdoor_number; i++)
            {
                CAAD_data.apb_and_ab_door.apb_level_list.Add(apb_level_list[i]);
            }
            int[] apb_reset_timestamp_list = new int[8];
            for (int i = 0; i < apb_reset_timestamp_list.Length; i++)
            {
                apb_reset_timestamp_list[i] = 0;
            }
            SQL = String.Format("SELECT d_e.apb_group_id AS apb_group_id,a_g_e.reset_time_1 AS r1,a_g_e.reset_time_2 AS r2,a_g_e.reset_time_3 AS r3,a_g_e.reset_time_4 AS r4,a_g_e.reset_time_5 AS r5,a_g_e.reset_time_6 AS r6,a_g_e.reset_time_7 AS r7,a_g_e.reset_time_8 AS r8 FROM door_extend AS d_e,door AS d,controller AS c,apb_group_extend AS a_g_e WHERE (a_g_e.apb_group_id=d_e.apb_group_id) AND (d_e.door_id=d.id) AND (d.controller_id=c.sn) AND c.sn={0} GROUP BY d_e.apb_group_id;", labSub000001_09.Text);
            MySqlDataReader Reader_Data3 = MySQL.GetDataReader(SQL);
            while (Reader_Data3.Read())
            {
                int id;
                DateTime r1, r2, r3, r4, r5, r6, r7, r8;
                id = Convert.ToInt32(Reader_Data3["apb_group_id"].ToString());
                r1 = Convert.ToDateTime(Reader_Data3["r1"].ToString());
                r2 = Convert.ToDateTime(Reader_Data3["r2"].ToString());
                r3 = Convert.ToDateTime(Reader_Data3["r3"].ToString());
                r4 = Convert.ToDateTime(Reader_Data3["r4"].ToString());
                r5 = Convert.ToDateTime(Reader_Data3["r5"].ToString());
                r6 = Convert.ToDateTime(Reader_Data3["r6"].ToString());
                r7 = Convert.ToDateTime(Reader_Data3["r7"].ToString());
                r8 = Convert.ToDateTime(Reader_Data3["r8"].ToString());
                apb_reset_timestamp_list[0] = (r1.Hour * 60 + r1.Minute) * 60;
                apb_reset_timestamp_list[1] = (r2.Hour * 60 + r2.Minute) * 60;
                apb_reset_timestamp_list[2] = (r3.Hour * 60 + r3.Minute) * 60;
                apb_reset_timestamp_list[3] = (r4.Hour * 60 + r4.Minute) * 60;
                apb_reset_timestamp_list[4] = (r5.Hour * 60 + r5.Minute) * 60;
                apb_reset_timestamp_list[5] = (r6.Hour * 60 + r6.Minute) * 60;
                apb_reset_timestamp_list[6] = (r7.Hour * 60 + r7.Minute) * 60;
                apb_reset_timestamp_list[7] = (r8.Hour * 60 + r8.Minute) * 60;
                break;
            }
            Reader_Data3.Close();
            for (int i = 0; i < apb_reset_timestamp_list.Length; i++)
            {
                CAAD_data.apb_and_ab_door.apb_reset_timestamp_list.Add(apb_reset_timestamp_list[i]);
            }
            return CAAD_data;
        }
        public CH_Controller UI_DB2CH_Controller(int intsy_dm_Controller_id, String controller_sn)//要把假日列表，依控制器獨立切開 at 2017/08/16 -- public CH_Controller UI_DB2CH_Controller(int intsy_dm_Controller_id)//Set Controller Holiday
        {
            int[] Holidays = HW_Net_API.Holiday2Array(controller_sn);//要把假日列表，依控制器獨立切開 at 2017/08/16 -- int[] Holidays = HW_Net_API.Holiday2Array();//呼叫SQL計算結果
            CH_Controller CH_data = new CH_Controller();
            CH_data.holiday = new CH_Holiday();
            CH_data.holiday.holiday_flags_list = new List<int>();
            CH_data.identifier = intsy_dm_Controller_id;
            CH_data.holiday.holiday_flags_list.Clear();
            for (int i = 0; i < 12; i++)
            {
                CH_data.holiday.holiday_flags_list.Add(Holidays[i]);
            }
            return CH_data;
        }
        public CC_Controller UI_DB2CC_Controller(int intsy_dm_Controller_id)//Set Controller Connection
        {
            CC_Controller CC_data = new CC_Controller();
            CC_data.connection = new CC_Connection();
            CC_data.identifier = intsy_dm_Controller_id;
            if (rdbSub000001_01.Checked)//控制器狀態
            {
                CC_data.connection.enabled = 1;
            }
            else
            {
                CC_data.connection.enabled = 0;
            }
            if (cmbSub000001_01.SelectedIndex > -1)//連線方式
            {
                CC_data.connection.mode = cmbSub000001_01.SelectedIndex;
            }
            else
            {
                cmbSub000001_01.SelectedIndex = 0;
                CC_data.connection.mode = cmbSub000001_01.SelectedIndex;
            }
            CC_data.connection.port = Convert.ToInt32(txtSub000001_03.Text);//PORT
            CC_data.connection.address = HW_Net_API.ip2long(txtSub000001_04.Text, true);//IP  //修正所有API內有關IP的運算公式變成32位元版-允許負數 //把IP轉換函數從32位元版改回64位元版-不允許有負數
            CC_data.connection.serial_number = Convert.ToInt64(labSub000001_09.Text, 16);//十六進位轉十進位
            return CC_data;

        }
        public int Getsy_dm_Controller_id(String StrSeachMode, String StrSeachIP, String StrSeachPort, bool blngetActive_0 = false)//修正Getsy_dm_Controller_id 函數和相關呼叫點-public int Getsy_dm_Controller_id(String StrSeachSN, String StrSeachIP, String StrSeachPort,bool blngetActive_0=false)
        {
            int intsy_dm_Controller_id = -1;

            //---
            //把控制器取得狀態改成一次一個SYDM，而非一台一台問
            m_intSycgControllerStatus_index = -1;
            //---把控制器取得狀態改成一次一個SYDM，而非一台一台問

            //--
            //找出Active=0 的重複利用 at 2017/08/18
            int intsy_dm_Controller_id_0 = -1;
            if (blngetActive_0==true)
            {
                //---
                //SYDM和SYCG API呼叫並存實現
                if (!m_changeSYCGMode)//SYDM
                {
                    HW_Net_API.getController_Active("/syris/sydm/controller/active?force=1");
                }
                else//SYCG
                {
                    m_blnAPI = HW_Net_API.SYCG_getSYDMList();
                    if (m_blnAPI)
                    {
                        HW_Net_API.m_Controller_Active.controllers.Clear();
                        for (int l = 0; l < m_Sydms.sydms.Count; l++)
                        {
                            HW_Net_API.SYCG_callSYDMCommand("SYDM_GET_CONTROLLER_ACTIVE", "\"force\":1", m_Sydms.sydms[l].identifier.ToString());
                        }
                    }
                }
                //---SYDM和SYCG API呼叫並存實現
                if (HW_Net_API.m_Controller_Active.controllers != null)
                {
                    for (int i = 0; i < HW_Net_API.m_Controller_Active.controllers.Count; i++)
                    {
                        if (HW_Net_API.m_Controller_Active.controllers[i].active == 0)
                        {
                            intsy_dm_Controller_id_0 = HW_Net_API.m_Controller_Active.controllers[i].identifier;
                            break;
                        }
                    }
                }
            }
            //--

            if (HW_Net_API.m_Controller_Connection.controllers != null && HW_Net_API.m_Controller_Connection.controllers.Count > 0)//會實際問控制器所以很慢，因此不用~if (HW_Net_API.m_get_Controller.controllers.Count > 0)
            {//sy_dm有控制器
                //MessageBox.Show("123-1");
                for (int i = 0; i < HW_Net_API.m_Controller_Connection.controllers.Count; i++)//會實際問控制器所以很慢，因此不用~for (int i = 0; i < HW_Net_API.m_get_Controller.controllers.Count; i++)
                {
                    String StrPort = "" + HW_Net_API.m_Controller_Connection.controllers[i].connection.port;
                    String StrIP = HW_Net_API.long2ip(HW_Net_API.m_Controller_Connection.controllers[i].connection.address, true); //修正所有API內有關IP的運算公式變成32位元版-允許負數 //把IP轉換函數從32位元版改回64位元版-不允許有負數
                    String StrMode = "" + HW_Net_API.m_Controller_Connection.controllers[i].connection.mode;//修正Getsy_dm_Controller_id 函數和相關呼叫點-String StrSN = Convert.ToString(HW_Net_API.m_Controller_Connection.controllers[i].connection.serial_number, 16);//API取得10進位值，比較時要再轉回16進位
                    String StrEnabled = "" + HW_Net_API.m_Controller_Connection.controllers[i].connection.enabled;
                    /*//停用 at 2017/0//07
                    if (StrEnabled == "1")
                    {
                        if ((StrSN == StrSeachSN) && (StrSeachPort == StrPort) && (StrSeachIP == StrIP))
                        {
                            intsy_dm_Controller_id = HW_Net_API.m_Controller_Connection.controllers[i].identifier;
                            break;
                        }
                    }
                    else
                    {
                        if ((StrSeachPort == StrPort) && (StrSeachIP == StrIP))
                        {
                            intsy_dm_Controller_id = HW_Net_API.m_Controller_Connection.controllers[i].identifier;
                            break;
                        }
                    }
                    */
                    if ((StrSeachPort == StrPort) && (StrSeachIP == StrIP) && (StrMode == StrSeachMode))//修正Getsy_dm_Controller_id 函數和相關呼叫點-if ((StrSeachPort == StrPort) && (StrSeachIP == StrIP))//只判斷IP/PORT，不判斷SN at 2017/0//07
                    {
                        intsy_dm_Controller_id = HW_Net_API.m_Controller_Connection.controllers[i].identifier;
                        //---
                        //把控制器取得狀態改成一次一個SYDM，而非一台一台問

                        //---
                        //狀態刷新的BUG-防止多SYDM時identifier重複無法分辨，手動新增
                        for(int j=0; j<HW_Net_API.m_Controller_Status.controllers.Count; j++)
                        {
                            if( (HW_Net_API.m_Controller_Connection.controllers[i].identifier == HW_Net_API.m_Controller_Status.controllers[j].identifier) && (HW_Net_API.m_Controller_Connection.controllers[i].sydm_id == HW_Net_API.m_Controller_Status.controllers[j].sydm_id) )
                            {
                                m_intSycgControllerStatus_index = j;
                                break;
                            }
                        }

                        //---狀態刷新的BUG-防止多SYDM時identifier重複無法分辨，手動新增

                        //---把控制器取得狀態改成一次一個SYDM，而非一台一台問
                        break;
                    }
                }
            }

            if (intsy_dm_Controller_id < 0 && intsy_dm_Controller_id_0 >0)
            {
                intsy_dm_Controller_id = intsy_dm_Controller_id_0;
            }

            return intsy_dm_Controller_id;
        }
        private void butSub000001_13_Click(object sender, EventArgs e)//呼叫增加控制器到sy_dm中 at 2017/08/01
        {
            if (ControlData2DB())//把設定控制器UI資料寫入DB中 at 2017/08/03
            {
                String SQL = "";
                bool blnAddController = true;//預設是呼叫新增控制器API
                int intsy_dm_Controller_id = -1;
                //---
                //SYDM和SYCG API呼叫並存實現
                //HW_Net_API.setAPIDomainURL(txtSys_01.Text, txtSys_07.Text);
                if (!m_changeSYCGMode)//SYDM
                {
                    HW_Net_API.setAPIDomainURL(txtSys_01.Text, txtSys_07.Text);
                }
                else//SYCG
                {
                    HW_Net_API.SYCG_setSYCGDomainURL();
                }
                //---SYDM和SYCG API呼叫並存實現

                //---
                //SYDM和SYCG API呼叫並存實現
                if (!m_changeSYCGMode)//SYDM
                {
                    m_blnAPI = HW_Net_API.getController_Connection();
                }
                else//SYCG
                {
                    m_blnAPI = HW_Net_API.SYCG_getSYDMList();
                    if (m_blnAPI)
                    {
                        HW_Net_API.m_Controller_Connection.controllers.Clear();
                        for (int l = 0; l < m_Sydms.sydms.Count; l++)
                        {
                            HW_Net_API.SYCG_callSYDMCommand("SYDM_GET_CONTROLLER_CONNECTION", "", m_Sydms.sydms[l].identifier.ToString());
                        }
                    }
                }
                //---SYDM和SYCG API呼叫並存實現	
                if (m_blnAPI)//if (HW_Net_API.getController_Connection())//實際聯結機器，太慢~if (HW_Net_API.getController())
                {
                    intsy_dm_Controller_id = Getsy_dm_Controller_id(""+cmbSub000001_01.SelectedIndex, txtSub000001_04.Text, txtSub000001_03.Text, true);//修正Getsy_dm_Controller_id 函數和相關呼叫點-intsy_dm_Controller_id = Getsy_dm_Controller_id(labSub000001_09.Text, txtSub000001_04.Text, txtSub000001_03.Text,true);//找出Active=0重複利用 at 2017/08/18
                    if (intsy_dm_Controller_id > 0)
                    {
                        blnAddController = false;
                    }

                    if (blnAddController)
                    {//呼叫新增控制器API
                        //MessageBox.Show("123-O");
                        //--
                        //Add Controller
                        Add_Controller AC_data = new Add_Controller();
                        AC_data.connection = new AC_Connection();
                        AC_data.apb_and_ab_door = new AC_ApbAndAbDoor();
                        AC_data.active = 1;//設定啟用
                        AC_data.connection.address = HW_Net_API.ip2long(txtSub000001_04.Text, true);//IP //修正所有API內有關IP的運算公式變成32位元版-允許負數 //把IP轉換函數從32位元版改回64位元版-不允許有負數
                        AC_data.connection.serial_number = Convert.ToInt64(labSub000001_09.Text, 16);//SN at 2017/08/07
                        if (rdbSub000001_01.Checked)//控制器狀態
                        {
                            AC_data.connection.enabled = 1;
                        }
                        else
                        {
                            AC_data.connection.enabled = 0;
                        }
                        AC_data.connection.port = Convert.ToInt32(txtSub000001_03.Text);//PORT
                        if (cmbSub000001_01.SelectedIndex > -1)//連線方式
                        {
                            AC_data.connection.mode = cmbSub000001_01.SelectedIndex;
                        }
                        else
                        {
                            cmbSub000001_01.SelectedIndex = 0;
                            AC_data.connection.mode = cmbSub000001_01.SelectedIndex;
                        }
                        if (ckbSub000001_01.Checked)
                        {
                            AC_data.apb_and_ab_door.apb_group = 0;//SQL  
                            AC_data.apb_and_ab_door.apb_enabled = 1;
                            SQL = String.Format("SELECT d_e.apb_group_id AS apb_group_id FROM door_extend AS d_e,door AS d,controller AS c WHERE (d_e.door_id=d.id) AND (d.controller_id=c.sn) AND c.sn={0};", labSub000001_09.Text);
                            MySqlDataReader Reader_Data = MySQL.GetDataReader(SQL);
                            while (Reader_Data.Read())
                            {
                                int tmp = Convert.ToInt32(Reader_Data["apb_group_id"].ToString());
                                if (tmp > 0)
                                {
                                    AC_data.apb_and_ab_door.apb_group = tmp;
                                }
                                break;
                            }
                            Reader_Data.Close();
                        }
                        else
                        {
                            AC_data.apb_and_ab_door.apb_enabled = 0;
                            AC_data.apb_and_ab_door.apb_group = 0;
                        }

			            //---
			            //SYDM和SYCG API呼叫並存實現
			            if(!m_changeSYCGMode)//SYDM
			            {
                            m_blnAPI = HW_Net_API.add_Controller(AC_data);
			            }
			            else//SYCG
			            {
				            //---
				            //SYCG模式下-建立/暫存 當下要操作的SYDM ID
				            SQL = String.Format("SELECT c.sydm_id AS sydm_id FROM controller AS c WHERE (c.sn='{0}')",labSub000001_09.Text);
				            MySqlDataReader Readerd_SYDMid = MySQL.GetDataReader(SQL);
				            while (Readerd_SYDMid.Read())
				            {
					            m_intSYDM_id = Convert.ToInt32(Readerd_SYDMid["sydm_id"].ToString());//SYCG模式下-建立/暫存 當下要操作的SYDM ID
					            break;
				            }
				            Readerd_SYDMid.Close();
				            //---SYCG模式下-建立/暫存 當下要操作的SYDM ID			
                            String StrAC_buf = parseJSON.composeJSON_Add_Controller(AC_data);		
                            m_blnAPI = HW_Net_API.SYCG_callSYDMCommand("SYDM_ADD_CONTROLLER", StrAC_buf, m_intSYDM_id.ToString());
			            }
			            //---SYDM和SYCG API呼叫並存實現
			            if (m_blnAPI)//if (HW_Net_API.add_Controller(AC_data))//新增控制器
                        {
                            //---
                            //SYDM和SYCG API呼叫並存實現
                            if (!m_changeSYCGMode)//SYDM
                            {
                                m_blnAPI = HW_Net_API.load_All_Controller();//重載控制器
                            }
                            else//SYCG
                            {
                                m_blnAPI = HW_Net_API.SYCG_callSYDMCommand("SYDM_LOAD_CONTROLLER", "", m_intSYDM_id.ToString());
                            }
                            //---SYDM和SYCG API呼叫並存實現
                            if(m_blnAPI)//if (HW_Net_API.load_All_Controller())//重載控制器
                            {
                                bool blnHaveController = false;
                                int intController_DM_id = -1;

                                //---
                                //SYDM和SYCG API呼叫並存實現
                                if (!m_changeSYCGMode)//SYDM
                                {
                                    m_blnAPI = HW_Net_API.getController_Connection();
                                }
                                else//SYCG
                                {
                                    m_blnAPI = HW_Net_API.SYCG_getSYDMList();
                                    if (m_blnAPI)
                                    {
                                        HW_Net_API.m_Controller_Connection.controllers.Clear();
                                        for (int l = 0; l < m_Sydms.sydms.Count; l++)
                                        {
                                            HW_Net_API.SYCG_callSYDMCommand("SYDM_GET_CONTROLLER_CONNECTION", "", m_Sydms.sydms[l].identifier.ToString());
                                        }
                                    }
                                }
                                //---SYDM和SYCG API呼叫並存實現	
                                if (m_blnAPI)//if (HW_Net_API.getController_Connection())//實際聯結機器，太慢~if (HW_Net_API.getController())
                                {
                                    /*停用 at 2017
                                    if (HW_Net_API.m_get_Controller.controllers.Count > 0)
                                    {//sy_dm有控制器
                                        //MessageBox.Show("123-1");
                                        for (int i = 0; i < HW_Net_API.m_get_Controller.controllers.Count; i++)
                                        {
                                            //Int32 i64IP = HW_Net_API.m_get_Controller.controllers[i].connection.address;
                                            //String StrIP = HW_Net_API.long2ip(HW_Net_API.m_get_Controller.controllers[i].connection.address, true);
                                            String StrSN = Convert.ToString(HW_Net_API.m_get_Controller.controllers[i].status.serial_number, 16);//API取得10進位值，比較時要再轉回16進位
                                            //MessageBox.Show("IP: " + StrIP + "\nSN: " + StrSN);
                                            if (StrSN == labSub000001_09.Text)//指判斷SN值~if ((StrIP == txtSub000001_04.Text) && (StrSN == labSub000001_09.Text))
                                            {
                                                blnHaveController = true;//找到剛才新增控制器
                                                intController_DM_id = HW_Net_API.m_get_Controller.controllers[i].identifier;
                                                break;
                                            }
                                        }//for
                                    }//if (HW_Net_API.m_get_Controller.controllers.Count > 0)
                                    */
                                    intController_DM_id = Getsy_dm_Controller_id("" + cmbSub000001_01.SelectedIndex, txtSub000001_04.Text, txtSub000001_03.Text);//修正Getsy_dm_Controller_id 函數和相關呼叫點-intController_DM_id = Getsy_dm_Controller_id(labSub000001_09.Text, txtSub000001_04.Text, txtSub000001_03.Text);
                                    if (intController_DM_id > 0)
                                    {

                                        //---
                                        //SYDM和SYCG API呼叫並存實現
                                        if (!m_changeSYCGMode)//SYDM
                                        {
                                            m_blnAPI = HW_Net_API.getController_Status(intController_DM_id);
                                        }
                                        else//SYCG
                                        {
                                            String StrGCS_buf = "\"identifier\":" + intController_DM_id;
                                            //---
                                            //SYCG模式下-建立/暫存 當下要操作的SYDM ID
                                            SQL = String.Format("SELECT c.sydm_id AS sydm_id FROM controller AS c WHERE (c.sn='{0}')", labSub000001_09.Text);
                                            MySqlDataReader Readerd_SYDMid = MySQL.GetDataReader(SQL);
                                            while (Readerd_SYDMid.Read())
                                            {
                                                m_intSYDM_id = Convert.ToInt32(Readerd_SYDMid["sydm_id"].ToString());//SYCG模式下-建立/暫存 當下要操作的SYDM ID
                                                break;
                                            }
                                            Readerd_SYDMid.Close();
                                            //---SYCG模式下-建立/暫存 當下要操作的SYDM ID				
                                            m_blnAPI = HW_Net_API.SYCG_callSYDMCommand("SYDM_GET_CONTROLLER_STATUS", StrGCS_buf, m_intSYDM_id.ToString());
                                        }
                                        //---SYDM和SYCG API呼叫並存實現
                                        if (m_blnAPI)//if (HW_Net_API.getController_Status(intController_DM_id))
                                        {
                                            if (HW_Net_API.m_Controller_Status.controllers != null)
                                            {
                                                if (HW_Net_API.m_Controller_Status.controllers[0].status.is_connected > 0)
                                                {
                                                    blnHaveController = true;//找到剛才新增控制器，且有連線
                                                }
                                            }
                                            else
                                            {
                                                blnHaveController = false;//找到剛才新增控制器，但未連線
                                            }
                                        }
                                        else
                                        {
                                            blnHaveController = false;//找到剛才新增控制器，但未連線
                                        }
                                    }//if (intController_DM_id > 0)
                                }//if (HW_Net_API.getController())
                                if ((blnHaveController == true) && (intController_DM_id > 0))
                                {
                                    bool[] blnAns = new bool[3];
                                    //--
                                    //Set Controller Setup
                                    CS_Controller CS_data = new CS_Controller();
                                    CS_data.identifier = intController_DM_id;
                                    CS_data.setup.same_card_interval_time_second = Convert.ToInt32(txtSub000001_07.Text);//add 2017/08/22

	                                //---
	                                //SYDM和SYCG API呼叫並存實現
	                                if(!m_changeSYCGMode)//SYDM
	                                {
		                                m_blnAPI = HW_Net_API.setController_Setup(CS_data);
	                                }
	                                else//SYCG
	                                {
		                                //---
		                                //SYCG模式下-建立/暫存 當下要操作的SYDM ID
                                        SQL = String.Format("SELECT c.sydm_id AS sydm_id FROM controller AS c WHERE (c.sn='{0}')", labSub000001_09.Text);
		                                MySqlDataReader Readerd_SYDMid = MySQL.GetDataReader(SQL);
		                                while (Readerd_SYDMid.Read())
		                                {
			                                m_intSYDM_id = Convert.ToInt32(Readerd_SYDMid["sydm_id"].ToString());//SYCG模式下-建立/暫存 當下要操作的SYDM ID
			                                break;
		                                }
		                                Readerd_SYDMid.Close();
		                                //---SYCG模式下-建立/暫存 當下要操作的SYDM ID
		                                String StrCS_buf = parseJSON.composeJSON_Controller_Setup(CS_data);
		                                m_blnAPI = HW_Net_API.SYCG_callSYDMCommand("SYDM_SET_CONTROLLER_SETUP", StrCS_buf, m_intSYDM_id.ToString());
	                                }
	                                //---SYDM和SYCG API呼叫並存實現
	                                blnAns[2] = m_blnAPI;

                                    //--
                                    //--
                                    //Set Controller Holiday
                                    CH_Controller CH_data = UI_DB2CH_Controller(intController_DM_id, labSub000001_09.Text);//要把假日列表，依控制器獨立切開 at 2017/08/16 -- CH_Controller CH_data = UI_DB2CH_Controller(intController_DM_id);

                                    //---
                                    //SYDM和SYCG API呼叫並存實現
                                    if (!m_changeSYCGMode)//SYDM
                                    {
                                        m_blnAPI = HW_Net_API.setController_Holiday(CH_data);
                                    }
                                    else//SYCG
                                    {
                                        //---
                                        //SYCG模式下-建立/暫存 當下要操作的SYDM ID
                                        SQL = String.Format("SELECT c.sydm_id AS sydm_id FROM controller AS c WHERE (c.sn='{0}')", labSub000001_09.Text);
                                        MySqlDataReader Readerd_SYDMid = MySQL.GetDataReader(SQL);
                                        while (Readerd_SYDMid.Read())
                                        {
                                            m_intSYDM_id = Convert.ToInt32(Readerd_SYDMid["sydm_id"].ToString());//SYCG模式下-建立/暫存 當下要操作的SYDM ID
                                            break;
                                        }
                                        Readerd_SYDMid.Close();
                                        //---SYCG模式下-建立/暫存 當下要操作的SYDM ID
                                        String StrCH_buf = parseJSON.composeJSON_Controller_Holiday(CH_data);
                                        m_blnAPI = HW_Net_API.SYCG_callSYDMCommand("SYDM_SET_CONTROLLER_HOLIDAY", StrCH_buf, m_intSYDM_id.ToString());
                                    }
                                    //---SYDM和SYCG API呼叫並存實現
                                    blnAns[0] = m_blnAPI;

                                    //--
                                    //--
                                    //Set Controller A.P.B & A/B
                                    CAAD_Controller CAAD_data = UI_DB2CAAD_Controller(intController_DM_id);

                                    //---
                                    //SYDM和SYCG API呼叫並存實現
                                    if (!m_changeSYCGMode)//SYDM
                                    {
                                        m_blnAPI = HW_Net_API.setController_Apb_Ab_Door(CAAD_data);
                                    }
                                    else//SYCG
                                    {
                                        //---
                                        //SYCG模式下-建立/暫存 當下要操作的SYDM ID
                                        SQL = String.Format("SELECT c.sydm_id AS sydm_id FROM controller AS c WHERE (c.sn='{0}')", labSub000001_09.Text);
                                        MySqlDataReader Readerd_SYDMid = MySQL.GetDataReader(SQL);
                                        while (Readerd_SYDMid.Read())
                                        {
                                            m_intSYDM_id = Convert.ToInt32(Readerd_SYDMid["sydm_id"].ToString());//SYCG模式下-建立/暫存 當下要操作的SYDM ID
                                            break;
                                        }
                                        Readerd_SYDMid.Close();
                                        //---SYCG模式下-建立/暫存 當下要操作的SYDM ID
                                        String StrCAAD_buf = parseJSON.composeJSON_Controller_Apb_Ab_Door(CAAD_data);
                                        m_blnAPI = HW_Net_API.SYCG_callSYDMCommand("SYDM_SET_CONTROLLER_APB_AB_DOOR", StrCAAD_buf, m_intSYDM_id.ToString());
                                    }
                                    //---SYDM和SYCG API呼叫並存實現
                                    blnAns[1] = m_blnAPI;

                                    //--

                                    if (blnAns[0] && blnAns[1] && blnAns[2])
                                    {
                                        MessageBox.Show(Language.m_StrbutSub000001_13Msg00, butSub000001_13.Text, MessageBoxButtons.OK, MessageBoxIcon.Information);
                                    }
                                    else
                                    {
                                        if (!blnAns[2])//add 2017/08/22
                                        {
                                            MessageBox.Show(Language.m_StrbutSub000001_13Msg01, butSub000001_13.Text, MessageBoxButtons.OK, MessageBoxIcon.Error);
                                        }

                                        if ((!blnAns[0]) && (!blnAns[1]))
                                        {
                                            MessageBox.Show(Language.m_StrbutSub000001_13Msg02, butSub000001_13.Text, MessageBoxButtons.OK, MessageBoxIcon.Error);
                                        }
                                        else if ((!blnAns[0]))
                                        {
                                            MessageBox.Show(Language.m_StrbutSub000001_13Msg03, butSub000001_13.Text, MessageBoxButtons.OK, MessageBoxIcon.Error);
                                        }
                                        else
                                        {
                                            MessageBox.Show(Language.m_StrbutSub000001_13Msg04, butSub000001_13.Text, MessageBoxButtons.OK, MessageBoxIcon.Error);
                                        }
                                    }
                                }
                                else
                                {
                                    MessageBox.Show(Language.m_StrbutSub000001_13Msg05, butSub000001_13.Text, MessageBoxButtons.OK, MessageBoxIcon.Error);
                                }
                            }
                            else
                            {
                                MessageBox.Show(Language.m_StrbutSub000001_13Msg06, butSub000001_13.Text, MessageBoxButtons.OK, MessageBoxIcon.Error);
                            }
                        }
                        else
                        {
                            MessageBox.Show(Language.m_StrbutSub000001_13Msg07, butSub000001_13.Text, MessageBoxButtons.OK, MessageBoxIcon.Error);
                        }
                        //--
                    }
                    else
                    {//呼叫修改控制器API
                        //MessageBox.Show("123-X");

                        //--
                        //Set Controller Active at 2017/08/18
                        CA_Controller CA_data = new CA_Controller();
                        CA_data.active = 1;
                        CA_data.identifier = intsy_dm_Controller_id;

                        //---
                        //SYDM和SYCG API呼叫並存實現
                        if (!m_changeSYCGMode)//SYDM
                        {
                            m_blnAPI = HW_Net_API.setController_Active(CA_data);
                        }
                        else//SYCG
                        {
                            String StrCA_buf = parseJSON.composeJSON_Controller_Active(CA_data);
                            m_blnAPI = HW_Net_API.SYCG_callSYDMCommand("SYDM_SET_CONTROLLER_ACTIVE", StrCA_buf, m_intSYDM_id.ToString());
                        }
                        //---SYDM和SYCG API呼叫並存實現
                        if (!m_blnAPI)//if (HW_Net_API.setController_Active(CA_data))
                        {
                            MessageBox.Show(Language.m_StrbutSub000001_13Msg08, butSub000001_13.Text, MessageBoxButtons.OK, MessageBoxIcon.Error);
                        }
                        //--

                        //--
                        //Set Controller Connection
                        CC_Controller CC_data = UI_DB2CC_Controller(intsy_dm_Controller_id);
                        //---
                        //SYDM和SYCG API呼叫並存實現
                        if (!m_changeSYCGMode)//SYDM
                        {
                            m_blnAPI = HW_Net_API.setController_Connection(CC_data);
                        }
                        else//SYCG
                        {
				            String StrCC_buf = parseJSON.composeJSON_Controller_Connection(CC_data);		
                            m_blnAPI = HW_Net_API.SYCG_callSYDMCommand("SYDM_SET_CONTROLLER_CONNECTION", StrCC_buf, m_intSYDM_id.ToString());
                        }
                        //---SYDM和SYCG API呼叫並存實現	
                        if (!m_blnAPI)//if (!HW_Net_API.setController_Connection(CC_data))
                        {
                            MessageBox.Show(Language.m_StrbutSub000001_13Msg09, butSub000001_13.Text, MessageBoxButtons.OK, MessageBoxIcon.Error);
                        }
                        //--

                        //--
                        //Load All Controller
                        //---
                        //SYDM和SYCG API呼叫並存實現
                        if (!m_changeSYCGMode)//SYDM
                        {
                            m_blnAPI = HW_Net_API.load_All_Controller();//重載控制器
                        }
                        else//SYCG
                        {
                            m_blnAPI = HW_Net_API.SYCG_callSYDMCommand("SYDM_LOAD_CONTROLLER", "", m_intSYDM_id.ToString());
                        }
                        //---SYDM和SYCG API呼叫並存實現
                        if (!m_blnAPI)//if (!HW_Net_API.load_All_Controller())//重載控制器
                        {
                            MessageBox.Show(Language.m_StrbutSub000001_13Msg10, butSub000001_13.Text, MessageBoxButtons.OK, MessageBoxIcon.Error);
                        }
                        //--

                        bool blnHaveController = false;
                        //---
                        //SYDM和SYCG API呼叫並存實現
                        if (!m_changeSYCGMode)//SYDM
                        {
                            m_blnAPI = HW_Net_API.getController_Status(intsy_dm_Controller_id);
                        }
                        else//SYCG
                        {
                            String StrGCS_buf = "\"identifier\":" + intsy_dm_Controller_id;
                            m_blnAPI = HW_Net_API.SYCG_callSYDMCommand("SYDM_GET_CONTROLLER_STATUS", StrGCS_buf, m_intSYDM_id.ToString());
                        }
                        //---SYDM和SYCG API呼叫並存實現
                        if (m_blnAPI)//if (HW_Net_API.getController_Status(intsy_dm_Controller_id))
                        {
                            if (HW_Net_API.m_Controller_Status.controllers != null)
                            {
                                if (HW_Net_API.m_Controller_Status.controllers[0].status.is_connected > 0)
                                {
                                    blnHaveController = true;//找到剛才新增控制器，且有連線
                                }
                                else
                                {
                                    MessageBox.Show(Language.m_StrbutSub000001_13Msg11, butSub000001_13.Text, MessageBoxButtons.OK, MessageBoxIcon.Error);
                                }
                            }
                            else
                            {
                                blnHaveController = false;//找到剛才新增控制器，但未連線
                                MessageBox.Show(Language.m_StrbutSub000001_13Msg12, butSub000001_13.Text, MessageBoxButtons.OK, MessageBoxIcon.Error);
                            }
                        }
                        else
                        {
                            blnHaveController = false;//找到剛才新增控制器，但未連線
                            MessageBox.Show(Language.m_StrbutSub000001_13Msg13, butSub000001_13.Text, MessageBoxButtons.OK, MessageBoxIcon.Error);
                        }

                        if (blnHaveController)
                        {
                            bool[] blnAns = new bool[3];
                            //--
                            //Set Controller Setup
                            CS_Controller CS_data = new CS_Controller();
                            CS_data.identifier = intsy_dm_Controller_id;
                            CS_data.setup.same_card_interval_time_second = Convert.ToInt32(txtSub000001_07.Text);//add 2017/08/22

	                        //---
	                        //SYDM和SYCG API呼叫並存實現
	                        if(!m_changeSYCGMode)//SYDM
	                        {
		                        m_blnAPI = HW_Net_API.setController_Setup(CS_data);
	                        }
	                        else//SYCG
	                        {
		                        //---
		                        //SYCG模式下-建立/暫存 當下要操作的SYDM ID
		                        SQL = String.Format("SELECT c.sydm_id AS sydm_id FROM controller AS c WHERE (c.sn='{0}')",labSub000001_09.Text);
		                        MySqlDataReader Readerd_SYDMid = MySQL.GetDataReader(SQL);
		                        while (Readerd_SYDMid.Read())
		                        {
			                        m_intSYDM_id = Convert.ToInt32(Readerd_SYDMid["sydm_id"].ToString());//SYCG模式下-建立/暫存 當下要操作的SYDM ID
			                        break;
		                        }
		                        Readerd_SYDMid.Close();
		                        //---SYCG模式下-建立/暫存 當下要操作的SYDM ID
		                        String StrCS_buf = parseJSON.composeJSON_Controller_Setup(CS_data);
		                        m_blnAPI = HW_Net_API.SYCG_callSYDMCommand("SYDM_SET_CONTROLLER_SETUP", StrCS_buf, m_intSYDM_id.ToString());
	                        }
	                        //---SYDM和SYCG API呼叫並存實現
	                        blnAns[2] = m_blnAPI;

                            //--
                            //--
                            //Set Controller Holiday
                            CH_Controller CH_data = UI_DB2CH_Controller(intsy_dm_Controller_id, labSub000001_09.Text);//要把假日列表，依控制器獨立切開 at 2017/08/16 -- CH_Controller CH_data = UI_DB2CH_Controller(intsy_dm_Controller_id);

	                        //---
	                        //SYDM和SYCG API呼叫並存實現
	                        if(!m_changeSYCGMode)//SYDM
	                        {
		                        m_blnAPI = HW_Net_API.setController_Holiday(CH_data);
	                        }
	                        else//SYCG
	                        {
		                        //---
		                        //SYCG模式下-建立/暫存 當下要操作的SYDM ID
                                SQL = String.Format("SELECT c.sydm_id AS sydm_id FROM controller AS c WHERE (c.sn='{0}')", labSub000001_09.Text);
		                        MySqlDataReader Readerd_SYDMid = MySQL.GetDataReader(SQL);
		                        while (Readerd_SYDMid.Read())
		                        {
			                        m_intSYDM_id = Convert.ToInt32(Readerd_SYDMid["sydm_id"].ToString());//SYCG模式下-建立/暫存 當下要操作的SYDM ID
			                        break;
		                        }
		                        Readerd_SYDMid.Close();
		                        //---SYCG模式下-建立/暫存 當下要操作的SYDM ID
		                        String StrCH_buf = parseJSON.composeJSON_Controller_Holiday(CH_data);
		                        m_blnAPI = HW_Net_API.SYCG_callSYDMCommand("SYDM_SET_CONTROLLER_HOLIDAY", StrCH_buf, m_intSYDM_id.ToString());
	                        }
	                        //---SYDM和SYCG API呼叫並存實現
	                        blnAns[0] = m_blnAPI;

                            //--
                            //--
                            //Set Controller A.P.B & A/B
                            CAAD_Controller CAAD_data = UI_DB2CAAD_Controller(intsy_dm_Controller_id);
                            
	                        //---
	                        //SYDM和SYCG API呼叫並存實現
	                        if(!m_changeSYCGMode)//SYDM
	                        {
		                        m_blnAPI = HW_Net_API.setController_Apb_Ab_Door(CAAD_data);
	                        }
	                        else//SYCG
	                        {
		                        //---
		                        //SYCG模式下-建立/暫存 當下要操作的SYDM ID
                                SQL = String.Format("SELECT c.sydm_id AS sydm_id FROM controller AS c WHERE (c.sn='{0}')", labSub000001_09.Text);
		                        MySqlDataReader Readerd_SYDMid = MySQL.GetDataReader(SQL);
		                        while (Readerd_SYDMid.Read())
		                        {
			                        m_intSYDM_id = Convert.ToInt32(Readerd_SYDMid["sydm_id"].ToString());//SYCG模式下-建立/暫存 當下要操作的SYDM ID
			                        break;
		                        }
		                        Readerd_SYDMid.Close();
		                        //---SYCG模式下-建立/暫存 當下要操作的SYDM ID
		                        String StrCAAD_buf = parseJSON.composeJSON_Controller_Apb_Ab_Door(CAAD_data);
		                        m_blnAPI = HW_Net_API.SYCG_callSYDMCommand("SYDM_SET_CONTROLLER_APB_AB_DOOR", StrCAAD_buf, m_intSYDM_id.ToString());
	                        }
	                        //---SYDM和SYCG API呼叫並存實現
                            blnAns[1] = m_blnAPI;

                            //--

                            if (blnAns[0] && blnAns[1])
                            {
                                MessageBox.Show(Language.m_StrbutSub000001_13Msg14, butSub000001_13.Text, MessageBoxButtons.OK, MessageBoxIcon.Information);
                            }
                            else
                            {
                                if (!blnAns[2])//add 2017/08/22
                                {
                                    MessageBox.Show(Language.m_StrbutSub000001_13Msg15, butSub000001_13.Text, MessageBoxButtons.OK, MessageBoxIcon.Error);
                                }

                                if ((!blnAns[0]) && (!blnAns[1]))
                                {
                                    MessageBox.Show(Language.m_StrbutSub000001_13Msg16, butSub000001_13.Text, MessageBoxButtons.OK, MessageBoxIcon.Error);
                                }
                                else if ((!blnAns[0]))
                                {
                                    MessageBox.Show(Language.m_StrbutSub000001_13Msg17, butSub000001_13.Text, MessageBoxButtons.OK, MessageBoxIcon.Error);
                                }
                                else
                                {
                                    MessageBox.Show(Language.m_StrbutSub000001_13Msg18, butSub000001_13.Text, MessageBoxButtons.OK, MessageBoxIcon.Error);
                                }
                            }
                        }
                    }
                }
                else
                {//無法連接sy_dm
                    MessageBox.Show(Language.m_StrbutSub000001_13Msg19, butSub000001_13.Text, MessageBoxButtons.OK, MessageBoxIcon.Error);
                }

                //---
                //控制器UI多選編輯實作 ~ 撰寫對應鍵盤事件

                //Leave_function();
                if (m_ALControllerObj.Count <= 1)
                {
                    Leave_function();
                }

                //---控制器UI多選編輯實作 ~ 撰寫對應鍵盤事件
            }
        }
        private void txtSub000001_03_KeyPress(object sender, KeyPressEventArgs e)//PORT只能輸入數字防呆
        {
            if (e.KeyChar == 8)//刪除鍵要直接允許
            {
                e.Handled = false;
            }
            else
            {
                if (e.KeyChar >= '0' && e.KeyChar <= '9')//限制0~9
                {
                    e.Handled = false;
                }
                else
                {
                    e.Handled = true;
                }
            }
        }

        private void txtSub000001_07_KeyUp(object sender, KeyEventArgs e)//限制在0~255
        {
            int temp = 0;
            try
            {
                temp = Convert.ToInt32(txtSub000001_07.Text);
            }
            catch
            {
                temp = 0;
            }
            if (!(temp >= 0 && temp <= 255))
            {
                temp = 0;
            }
            txtSub000001_07.Text = "" + temp;
        }

        private void txtSub000001_03_KeyUp(object sender, KeyEventArgs e)//PORT限制在1~65535
        {
            int temp=0;
            try
            {
                temp=Convert.ToInt32(txtSub000001_03.Text);
            }
            catch
            {
                temp=5001;
            }
            if (!(temp >= 1 && temp <= 65535))
            {
                temp = 5001;
            }
            txtSub000001_03.Text = "" + temp;
        }

        private void labSub000001_09_KeyPress(object sender, KeyPressEventArgs e)//序號限制數字+長度
        {
            if (e.KeyChar == 8)//刪除鍵要直接允許
            {
                e.Handled = false;
            }
            else
            {
                if (labSub000001_09.Text.Length < 8)//長度限制在8
                {
                    if (e.KeyChar >= '0' && e.KeyChar <= '9')//限制0~9和A~F
                    {
                        e.Handled = false;
                    }
                    else
                    {
                        e.Handled = true;
                    }
                }
                else
                {
                    e.Handled = true;
                }
            }
        }

        private void ckbSub000001_01_CheckedChanged(object sender, EventArgs e)
        {
            rdbSub000001_03.Enabled = ((CheckBox)sender).Checked;
            rdbSub000001_04.Enabled = ((CheckBox)sender).Checked;
        }

        private void ckbSub000001_02_CheckedChanged(object sender, EventArgs e)
        {
            txtSub000001_19.Enabled = ((CheckBox)sender).Checked;
            ChangeSub000001UI(true);//用來切換子元件顯示-2017/03/03
        }

        private void cmbSub000001_03_SelectedIndexChanged(object sender, EventArgs e)//控制器選擇-2017/03/03
        {
            ChangeSub000001UI(true);//用來切換子元件顯示-2017/03/03
        }
        private void rdbSub000001_01_CheckedChanged(object sender, EventArgs e)//啟用於否事件[rdbSub000001_01和rdbSub000001_02都指向這個函數]-2017/03/03 
        {
            ChangeSub000001UI();//用來切換子元件顯示-2017/03/03
        }
        private void txtSub000001_19_Value_Changed(object sender, EventArgs e)
        {
            ChangeSub000001UI(true);//用來切換子元件顯示-2017/03/03
        }

        private void cmbSub000001_01_SelectedIndexChanged(object sender, EventArgs e)
        {
            //---
            //控制器在SERVER mode 時 SN 自動填入 日+時+分+秒
            if (cmbSub000001_01.Enabled == true)
            {
                switch (cmbSub000001_01.SelectedIndex)
                {
                    case 0://SERVER
                        labSub000001_09.Text = DateTime.Now.ToString("ddHHmmss");
                        labSub000001_09.Enabled = false;
                        break;
                    case 1://CLIENT
                        labSub000001_09.Text = "";
                        labSub000001_09.Enabled = true;
                        break;
                }
            }
            //---控制器在SERVER mode 時 SN 自動填入 日+時+分+秒
        }

        private void dgvSub000001_01_SelectionChanged(object sender, EventArgs e)
        {
            //---
            //列表元件shift全選時最前面的狀態要能自動打勾，反之要能取消
            for (int i = 0; i < dgvSub000001_01.Rows.Count; i++)
            {
                dgvSub000001_01.Rows[i].Cells[0].Value = false;
            }
            for (int j = 0; j < dgvSub000001_01.SelectedRows.Count; j++)
            {
                dgvSub000001_01.SelectedRows[j].Cells[0].Value = true;
            }
            //---列表元件shift全選時最前面的狀態要能自動打勾，反之要能取消
        }
        //--Sub000001_end

        //--Sub000100_start
        private void ckbSub000100_05_CheckedChanged(object sender, EventArgs e)
        {
            txtSub000100_04.Enabled = ckbSub000100_05.Checked;
            txtSub000100_05.Enabled = ckbSub000100_05.Checked;
            if (!ckbSub000100_05.Checked)
            {
                txtSub000100_04.Value = 0;
                txtSub000100_05.Value = 0;
            }
        }

        private void rdbSub000100_03_CheckedChanged(object sender, EventArgs e)
        {
            rdbSub000100_07.Enabled = rdbSub000100_03.Checked;
            rdbSub000100_08.Enabled = rdbSub000100_03.Checked;
        }

        private void ckbSub000100_06_CheckedChanged(object sender, EventArgs e)
        {
            ckbSub000100_07.Enabled = ckbSub000100_06.Checked;
            txtSub000100_13.Enabled = ckbSub000100_06.Checked;
            txtSub000100_14.Enabled = ckbSub000100_06.Checked;
        }

        private void ckbSub000100_08_CheckedChanged(object sender, EventArgs e)
        {
            ckbSub000100_09.Enabled = ckbSub000100_08.Checked;
            ckbSub000100_10.Enabled = ckbSub000100_08.Checked;
            txtSub000100_15.Enabled = ckbSub000100_08.Checked;
            txtSub000100_16.Enabled = ckbSub000100_08.Checked;
        }

        private void ckbSub000100_11_CheckedChanged(object sender, EventArgs e)
        {
            txtSub000100_18.Enabled = ckbSub000100_11.Checked;
            txtSub000100_19.Enabled = ckbSub000100_11.Checked;
            txtSub000100_20.Enabled = ckbSub000100_11.Checked;
            cmbSub000100_01.Enabled = ckbSub000100_11.Checked;
        }

        private void ckbSub000100_12_CheckedChanged(object sender, EventArgs e)
        {
            txtSub000100_21.Enabled = ckbSub000100_12.Checked;
        }

        private void ckbSub000100_13_CheckedChanged(object sender, EventArgs e)
        {
            txtSub000100_22.Enabled = ckbSub000100_13.Checked;
            txtSub000100_23.Enabled = ckbSub000100_13.Checked;
            txtSub000100_24.Enabled = ckbSub000100_13.Checked;
            cmbSub000100_02.Enabled = ckbSub000100_13.Checked;
        }

        private void ckbSub000100_14_CheckedChanged(object sender, EventArgs e)
        {
            txtSub000100_25.Enabled = ckbSub000100_14.Checked;
            txtSub000100_26.Enabled = ckbSub000100_14.Checked;
            txtSub000100_27.Enabled = ckbSub000100_14.Checked;
            cmbSub000100_03.Enabled = ckbSub000100_14.Checked;
        }

        private void ckbSub000100_49_CheckedChanged(object sender, EventArgs e)
        {
            txtSub000100_28.Enabled = ckbSub000100_49.Checked;
            txtSub000100_29.Enabled = ckbSub000100_49.Checked;
            txtSub000100_30.Enabled = ckbSub000100_49.Checked;
            cmbSub000100_04.Enabled = ckbSub000100_49.Checked;
        }

        private void ckbSub000100_31_CheckedChanged(object sender, EventArgs e)
        {
            rdbSub000100_23.Enabled = ckbSub000100_31.Checked;
            rdbSub000100_24.Enabled = ckbSub000100_31.Checked;
            ckbSub000100_32.Enabled = ckbSub000100_31.Checked;
            ckbSub000100_33.Enabled = ckbSub000100_31.Checked;
            ckbSub000100_34.Enabled = ckbSub000100_31.Checked;
            ckbSub000100_35.Enabled = ckbSub000100_31.Checked;
            ckbSub000100_36.Enabled = ckbSub000100_31.Checked;
            ckbSub000100_37.Enabled = ckbSub000100_31.Checked;
            ckbSub000100_38.Enabled = ckbSub000100_31.Checked;
            ckbSub000100_39.Enabled = ckbSub000100_31.Checked;
            steckbSub000100_01.Enabled = ckbSub000100_31.Checked;
            steckbSub000100_02.Enabled = ckbSub000100_31.Checked;
            steckbSub000100_03.Enabled = ckbSub000100_31.Checked;
            steckbSub000100_04.Enabled = ckbSub000100_31.Checked;
            steckbSub000100_05.Enabled = ckbSub000100_31.Checked;
            steckbSub000100_06.Enabled = ckbSub000100_31.Checked;
            steckbSub000100_07.Enabled = ckbSub000100_31.Checked;
            steckbSub000100_08.Enabled = ckbSub000100_31.Checked;
        }

        private void ckbSub000100_40_CheckedChanged(object sender, EventArgs e)
        {
            rdbSub000100_25.Enabled = ckbSub000100_40.Checked;
            rdbSub000100_26.Enabled = ckbSub000100_40.Checked;
            rdbSub000100_27.Enabled = ckbSub000100_40.Checked;
            ckbSub000100_41.Enabled = ckbSub000100_40.Checked;
            ckbSub000100_42.Enabled = ckbSub000100_40.Checked;
            ckbSub000100_43.Enabled = ckbSub000100_40.Checked;
            ckbSub000100_44.Enabled = ckbSub000100_40.Checked;
            ckbSub000100_45.Enabled = ckbSub000100_40.Checked;
            ckbSub000100_46.Enabled = ckbSub000100_40.Checked;
            ckbSub000100_47.Enabled = ckbSub000100_40.Checked;
            ckbSub000100_48.Enabled = ckbSub000100_40.Checked;
            steckbSub000100_09.Enabled = ckbSub000100_40.Checked;
            steckbSub000100_10.Enabled = ckbSub000100_40.Checked;
            steckbSub000100_11.Enabled = ckbSub000100_40.Checked;
            steckbSub000100_12.Enabled = ckbSub000100_40.Checked;
            steckbSub000100_13.Enabled = ckbSub000100_40.Checked;
            steckbSub000100_14.Enabled = ckbSub000100_40.Checked;
            steckbSub000100_15.Enabled = ckbSub000100_40.Checked;
            steckbSub000100_16.Enabled = ckbSub000100_40.Checked;
        }

        public void DoorData2DB(int State = 1)
        {
            Sub000100_getUIValue();
            String SQL;
            if (!m_blnSub000100modified)
            {
                SQL = String.Format(@"INSERT INTO door_extend (door_id,base,pass,open,anti_de,detect,button,anti_co,overtime,violent,pass_mode,auto_mode,access_den,state) VALUES ({0},'{1}','{2}','{3}','{4}','{5}','{6}','{7}','{8}','{9}','{10}','{11}','{12}',{13});",
                                    m_StrSub000100door_id,
                                    m_StrBase,
                                    m_StrPass,
                                    m_StrOpen,
                                    m_StrAnti_de,
                                    m_StrDetect,
                                    m_StrButton,
                                    m_StrAnti_co,
                                    m_StrOvertime,
                                    m_StrViolent,
                                    m_StrPass_mode,
                                    m_StrAuto_mode,
                                    m_StrAccess_den,
                                    State);

                m_blnSub000100modified = true;
            }
            else
            {
                SQL = String.Format(@"UPDATE door_extend SET base='{0}',pass='{1}',open='{2}',anti_de='{3}',detect='{4}',button='{5}',anti_co='{6}',overtime='{7}',violent='{8}',pass_mode='{9}',auto_mode='{10}',access_den='{11}',state={13} WHERE door_id={12};",
                                    m_StrBase,
                                    m_StrPass,
                                    m_StrOpen,
                                    m_StrAnti_de,
                                    m_StrDetect,
                                    m_StrButton,
                                    m_StrAnti_co,
                                    m_StrOvertime,
                                    m_StrViolent,
                                    m_StrPass_mode,
                                    m_StrAuto_mode,
                                    m_StrAccess_den,
                                    m_StrSub000100door_id,
                                    State);
            }
            MySQL.InsertUpdateDelete(SQL);//新增資料程式

            //---
            //按照『V8 功能選單』一個一個改 - 設置門區/電梯 ~ 要可修改門區名稱[只有門才能修改]
            if(txtSub000100_01.ReadOnly == false)
            {
                SQL = String.Format("UPDATE door SET name='{0}' WHERE id={1};", txtSub000100_01.Text, m_StrSub000100door_id);
                MySQL.InsertUpdateDelete(SQL);//新增資料程式
                labSub000100.Text = Language.m_StrTabPageTag000101 + "-" + txtSub000100_01.Text;

                initvmSub0001_01();
                initltvSub0001_01();
            }
            //---按照『V8 功能選單』一個一個改 - 設置門區/電梯 ~ 要可修改門區名稱[只有門才能修改]

            m_StrSub000100door_id = "-1";
        }
        private void butSub000100_10_Click(object sender, EventArgs e)//從sy_dm載入線上門區資訊
        {
            String SQL = "";
            String Strid = "", Strmode = "", Strip = "", Strport = "";//修正Getsy_dm_Controller_id 函數和相關呼叫點-String Strid = "", Strsn = "", Strip = "", Strport = "";
            String Strsn = "";
            bool blnHaveSy_dm = false;//無連接Sy_dm 
            //---
            //SYDM和SYCG API呼叫並存實現
            //HW_Net_API.setAPIDomainURL(txtSys_01.Text, txtSys_07.Text);
            if (!m_changeSYCGMode)//SYDM
            {
                HW_Net_API.setAPIDomainURL(txtSys_01.Text, txtSys_07.Text);
            }
            else//SYCG
            {
                HW_Net_API.SYCG_setSYCGDomainURL();
            }
            //---SYDM和SYCG API呼叫並存實現

            //---
            //SYDM和SYCG API呼叫並存實現
            if (!m_changeSYCGMode)//SYDM
            {
                m_blnAPI = HW_Net_API.getController_Connection();
            }
            else//SYCG
            {
                m_blnAPI = HW_Net_API.SYCG_getSYDMList();
                if (m_blnAPI)
                {
                    HW_Net_API.m_Controller_Connection.controllers.Clear();
                    for (int l = 0; l < m_Sydms.sydms.Count; l++)
                    {
                        HW_Net_API.SYCG_callSYDMCommand("SYDM_GET_CONTROLLER_CONNECTION", "", m_Sydms.sydms[l].identifier.ToString());
                    }
                }
            }
            //---SYDM和SYCG API呼叫並存實現	
            if (m_blnAPI)//if (HW_Net_API.getController_Connection())//實際聯結機器，太慢~if (HW_Net_API.getController())
            {
                blnHaveSy_dm = true;//有連接Sy_dm 
            }

            SQL = String.Format("SELECT d.controller_door_index AS id,c_e.controller_sn AS sn,c_e.connetction_mode AS mode,c_e.connetction_address AS ip,c_e.port as port FROM door AS d,controller_extend AS c_e WHERE (d.controller_id=c_e.controller_sn) AND (d.id={0});", m_StrSub000100door_id);//修正Getsy_dm_Controller_id 函數和相關呼叫點-SQL = String.Format("SELECT d.controller_door_index AS id,d.controller_id AS sn,c_e.connetction_address AS ip,c_e.port as port FROM door AS d,controller_extend AS c_e WHERE (d.controller_id=controller_sn) AND (d.id={0});", m_StrSub000100door_id);
            MySqlDataReader DataReader = MySQL.GetDataReader(SQL);
            while (DataReader.Read())
            {
                Strid = DataReader["id"].ToString();
                Strmode = DataReader["mode"].ToString();//修正Getsy_dm_Controller_id 函數和相關呼叫點-Strsn = DataReader["sn"].ToString();
                Strip = DataReader["ip"].ToString();
                Strport = DataReader["port"].ToString();
                Strsn = DataReader["sn"].ToString();
                break;
            }
            DataReader.Close();
            if (Strid != "0")//表示資料庫有值
            {
                if (blnHaveSy_dm)//有連接Sy_dm 
                {
                    int intsy_dm_Controller_id = -1;
                    int intsy_dm_Door_id = -1;
                    intsy_dm_Controller_id = Getsy_dm_Controller_id(Strmode, Strip, Strport);//修正Getsy_dm_Controller_id 函數和相關呼叫點-intsy_dm_Controller_id = Getsy_dm_Controller_id(Strsn, Strip, Strport);
                    if (intsy_dm_Controller_id > 0)//控制器有在Sy_dm 中
                    {

                        //---
                        //SYDM和SYCG API呼叫並存實現
                        if (!m_changeSYCGMode)//SYDM
                        {
                            m_blnAPI = HW_Net_API.getDoorTopology(intsy_dm_Controller_id, Convert.ToInt32(Strid));
                        }
                        else//SYCG
                        {
                            String StrGDT_buf = "\"controller_identifier\":" + intsy_dm_Controller_id + ",\"controller_door_index\":" + Strid;
                            //---
                            //SYCG模式下-建立/暫存 當下要操作的SYDM ID
                            SQL = String.Format("SELECT c.sydm_id AS sydm_id FROM controller AS c WHERE (c.sn='{0}')", Strsn);
                            MySqlDataReader Readerd_SYDMid = MySQL.GetDataReader(SQL);
                            while (Readerd_SYDMid.Read())
                            {
                                m_intSYDM_id = Convert.ToInt32(Readerd_SYDMid["sydm_id"].ToString());//SYCG模式下-建立/暫存 當下要操作的SYDM ID
                                break;
                            }
                            Readerd_SYDMid.Close();
                            //---SYCG模式下-建立/暫存 當下要操作的SYDM ID					
                            m_blnAPI = HW_Net_API.SYCG_callSYDMCommand("SYDM_GET_DOOR_TOPOLOGY", StrGDT_buf, m_intSYDM_id.ToString());
                        }
                        //---SYDM和SYCG API呼叫並存實現
                        if (m_blnAPI)//if (HW_Net_API.getDoorTopology(intsy_dm_Controller_id, Convert.ToInt32(Strid)))//抓取門區拓譜
                        {
                            if (HW_Net_API.m_Door_Topology.doors != null)//
                            {
                                try
                                {
                                    intsy_dm_Door_id = HW_Net_API.m_Door_Topology.doors[0].identifier;//抓取門區唯一值
                                }
                                catch
                                {
                                    intsy_dm_Door_id = -1;
                                }
                                if (intsy_dm_Door_id > 0)//準備讀取該門區參數
                                {

                                    //---
                                    //SYDM和SYCG API呼叫並存實現
                                    if (!m_changeSYCGMode)//SYDM
                                    {
                                        HW_Net_API.getDoorSecurity(intsy_dm_Door_id);
                                        HW_Net_API.getDoorSetup(intsy_dm_Door_id);
                                        HW_Net_API.getDoorTimePeriodControl(intsy_dm_Door_id);
                                        HW_Net_API.getDoorAutoOpenTimePeriodControl(intsy_dm_Door_id);
                                    }
                                    else//SYCG
                                    {
                                        String StrGDAll_buf = "\"identifier\":" + intsy_dm_Door_id;

                                        HW_Net_API.SYCG_callSYDMCommand("SYDM_GET_DOOR_SECURITY", StrGDAll_buf, m_intSYDM_id.ToString());
                                        HW_Net_API.SYCG_callSYDMCommand("SYDM_GET_DOOR_SETUP", StrGDAll_buf, m_intSYDM_id.ToString());
                                        HW_Net_API.SYCG_callSYDMCommand("SYDM_GET_DOOR_TIME_PERIOD_CONTROL", StrGDAll_buf, m_intSYDM_id.ToString());
                                        HW_Net_API.SYCG_callSYDMCommand("SYDM_GET_DOOR_AUTO_OPEN_TIME_PERIOD_CONTROL", StrGDAll_buf, m_intSYDM_id.ToString());
                                    }
                                    //---SYDM和SYCG API呼叫並存實現

                                    DoorSydm2DoorDB(0,1);
                                    Sub000100_setUIValue();
                                    MessageBox.Show(Language.m_StrbutSub000100_10Msg00, butSub000100_10.Text, MessageBoxButtons.OK, MessageBoxIcon.Information);
                                }
                                else
                                {
                                    MessageBox.Show(Language.m_StrbutSub000100_10Msg01, butSub000100_10.Text, MessageBoxButtons.OK, MessageBoxIcon.Error);
                                }
                            }
                            else
                            {
                                MessageBox.Show(Language.m_StrbutSub000100_10Msg02, butSub000100_10.Text, MessageBoxButtons.OK, MessageBoxIcon.Error);
                            }
                        }
                        else
                        {
                            MessageBox.Show(Language.m_StrbutSub000100_10Msg03, butSub000100_10.Text, MessageBoxButtons.OK, MessageBoxIcon.Error);
                        }
                    }
                    else
                    {
                        MessageBox.Show(Language.m_StrbutSub000100_10Msg04, butSub000100_10.Text, MessageBoxButtons.OK, MessageBoxIcon.Error);
                    }
                }
                else
                {
                    MessageBox.Show(Language.m_StrbutSub000100_10Msg05, butSub000100_10.Text, MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
            }
        }

        private void butSub000100_08_Click(object sender, EventArgs e)//門區參數傳送到sy_dm
        {
            String SQL="";
            String Strid = "", Strmode = "", Strip = "", Strport = "";//修正Getsy_dm_Controller_id 函數和相關呼叫點-String Strid="",Strsn="",Strip="",Strport="";
            String Strsn = "";
            bool blnHaveSy_dm = false;//無連接Sy_dm 

            //---
            //SYDM和SYCG API呼叫並存實現
            //HW_Net_API.setAPIDomainURL(txtSys_01.Text, txtSys_07.Text);
            if (!m_changeSYCGMode)//SYDM
            {
                HW_Net_API.setAPIDomainURL(txtSys_01.Text, txtSys_07.Text);
            }
            else//SYCG
            {
                HW_Net_API.SYCG_setSYCGDomainURL();
            }
            //---SYDM和SYCG API呼叫並存實現

            //---
            //SYDM和SYCG API呼叫並存實現
            if (!m_changeSYCGMode)//SYDM
            {
                m_blnAPI = HW_Net_API.getController_Connection();
            }
            else//SYCG
            {
                m_blnAPI = HW_Net_API.SYCG_getSYDMList();
                if (m_blnAPI)
                {
                    HW_Net_API.m_Controller_Connection.controllers.Clear();
                    for (int l = 0; l < m_Sydms.sydms.Count; l++)
                    {
                        HW_Net_API.SYCG_callSYDMCommand("SYDM_GET_CONTROLLER_CONNECTION", "", m_Sydms.sydms[l].identifier.ToString());
                    }
                }
            }
            //---SYDM和SYCG API呼叫並存實現	
            if (m_blnAPI)//if (HW_Net_API.getController_Connection())//實際聯結機器，太慢~if (HW_Net_API.getController())
            {
                blnHaveSy_dm = true;//有連接Sy_dm 
            }

            SQL = String.Format("SELECT d.controller_door_index AS id,c_e.controller_sn AS sn,c_e.connetction_mode AS mode,c_e.connetction_address AS ip,c_e.port as port FROM door AS d,controller_extend AS c_e WHERE (d.controller_id=c_e.controller_sn) AND (d.id={0});", m_StrSub000100door_id);//修正Getsy_dm_Controller_id 函數和相關呼叫點-SQL = String.Format("SELECT d.controller_door_index AS id,d.controller_id AS sn,c_e.connetction_address AS ip,c_e.port as port FROM door AS d,controller_extend AS c_e WHERE (d.controller_id=controller_sn) AND (d.id={0});", m_StrSub000100door_id);
            MySqlDataReader DataReader = MySQL.GetDataReader(SQL);
            while (DataReader.Read())
            {
                Strid = DataReader["id"].ToString();
                Strmode = DataReader["mode"].ToString();//修正Getsy_dm_Controller_id 函數和相關呼叫點-Strsn = DataReader["sn"].ToString();
                Strip = DataReader["ip"].ToString();
                Strport = DataReader["port"].ToString();
                Strsn = DataReader["sn"].ToString();
                break;
            }
            DataReader.Close();

            if (Strid != "0")//表示資料庫有值
            {
                if (blnHaveSy_dm)//有連接Sy_dm 
                {
                    int intsy_dm_Controller_id = -1;
                    int intsy_dm_Door_id = -1;
                    intsy_dm_Controller_id = Getsy_dm_Controller_id(Strmode, Strip, Strport);//修正Getsy_dm_Controller_id 函數和相關呼叫點-intsy_dm_Controller_id = Getsy_dm_Controller_id(Strsn, Strip, Strport);
                    if (intsy_dm_Controller_id > 0)//控制器有在Sy_dm 中
                    {

                        //---
                        //SYDM和SYCG API呼叫並存實現
                        if (!m_changeSYCGMode)//SYDM
                        {
                            m_blnAPI = HW_Net_API.getDoorTopology(intsy_dm_Controller_id, Convert.ToInt32(Strid));
                        }
                        else//SYCG
                        {
                            String StrGDT_buf = "\"controller_identifier\":" + intsy_dm_Controller_id + ",\"controller_door_index\":" + Strid;
                            //---
                            //SYCG模式下-建立/暫存 當下要操作的SYDM ID
                            SQL = String.Format("SELECT c.sydm_id AS sydm_id FROM controller AS c WHERE (c.sn='{0}')", Strsn);
                            MySqlDataReader Readerd_SYDMid = MySQL.GetDataReader(SQL);
                            while (Readerd_SYDMid.Read())
                            {
                                m_intSYDM_id = Convert.ToInt32(Readerd_SYDMid["sydm_id"].ToString());//SYCG模式下-建立/暫存 當下要操作的SYDM ID
                                break;
                            }
                            Readerd_SYDMid.Close();
                            //---SYCG模式下-建立/暫存 當下要操作的SYDM ID					
                            m_blnAPI = HW_Net_API.SYCG_callSYDMCommand("SYDM_GET_DOOR_TOPOLOGY", StrGDT_buf, m_intSYDM_id.ToString());
                        }
                        //---SYDM和SYCG API呼叫並存實現
                        if (m_blnAPI)//if (HW_Net_API.getDoorTopology(intsy_dm_Controller_id, Convert.ToInt32(Strid)))//抓取門區拓譜
                        {
                            if (HW_Net_API.m_Door_Topology.doors != null)//
                            {
                                try
                                {
                                    intsy_dm_Door_id = HW_Net_API.m_Door_Topology.doors[0].identifier;//抓取門區唯一值
                                }
                                catch
                                {
                                    intsy_dm_Door_id = -1;
                                }
                                if (intsy_dm_Door_id > 0)
                                {//設定門區參數
                                    bool[] blnAns = new bool[4];
                                    DoorData2DB();//有呼叫Sub000100_getUIValue();

                                    m_Door_Security.identifier = intsy_dm_Door_id;
                                    m_Door_Setup.identifier = intsy_dm_Door_id;
                                    m_Door_TimePeriodControl.identifier = intsy_dm_Door_id;
                                    m_Door_AutoOpenTimePeriodControl.identifier = intsy_dm_Door_id;

                                    //---
                                    //SYDM和SYCG API呼叫並存實現
                                    if (!m_changeSYCGMode)//SYDM
                                    {
                                        blnAns[0] = HW_Net_API.setDoorSecurity(m_Door_Security);
                                        blnAns[1] = HW_Net_API.setDoorSetup(m_Door_Setup);
                                        blnAns[2] = HW_Net_API.setDoorTimePeriodControl(m_Door_TimePeriodControl);
                                        blnAns[3] = HW_Net_API.setDoorAutoOpenTimePeriodControl(m_Door_AutoOpenTimePeriodControl);
                                    }
                                    else//SYCG
                                    {
                                        String StrSDAll_buf = "";

                                        StrSDAll_buf = parseJSON.composeJSON_Door_Security(m_Door_Security);
                                        blnAns[0] = HW_Net_API.SYCG_callSYDMCommand("SYDM_SET_DOOR_SECURITY", StrSDAll_buf, m_intSYDM_id.ToString());

                                        StrSDAll_buf = parseJSON.composeJSON_Door_Setup(m_Door_Setup);
                                        blnAns[1] = HW_Net_API.SYCG_callSYDMCommand("SYDM_SET_DOOR_SETUP", StrSDAll_buf, m_intSYDM_id.ToString());

                                        StrSDAll_buf = parseJSON.composeJSON_Door_TimePeriodControl(m_Door_TimePeriodControl);
                                        blnAns[2] = HW_Net_API.SYCG_callSYDMCommand("SYDM_SET_DOOR_TIME_PERIOD_CONTROL", StrSDAll_buf, m_intSYDM_id.ToString());

                                        StrSDAll_buf = parseJSON.composeJSON_Door_AutoOpenTimePeriodControl(m_Door_AutoOpenTimePeriodControl);
                                        blnAns[3] = HW_Net_API.SYCG_callSYDMCommand("SYDM_SET_DOOR_AUTO_OPEN_TIME_PERIOD_CONTROL", StrSDAll_buf, m_intSYDM_id.ToString());
                                    }
                                    //---SYDM和SYCG API呼叫並存實現

                                    if (blnAns[0] && blnAns[1] && blnAns[2] && blnAns[3])
                                    {
                                        MessageBox.Show(Language.m_StrbutSub000100_08Msg00, butSub000100_08.Text, MessageBoxButtons.OK, MessageBoxIcon.Information);
                                    }
                                    else
                                    {
                                        String StrMsg = "";
                                        if (!blnAns[0])
                                        {
                                            StrMsg += "setDoorSecurity，";
                                        }
                                        if (!blnAns[1])
                                        {
                                            StrMsg += "setDoorSetup，";
                                        }
                                        if (!blnAns[2])
                                        {
                                            StrMsg += "setDoorTimePeriodControl，";
                                        }
                                        if (!blnAns[3])
                                        {
                                            StrMsg += "setDoorAutoOpenTimePeriodControl，";
                                        }
                                        MessageBox.Show(Language.m_StrbutSub000100_08Msg01 + StrMsg + Language.m_StrbutSub000100_08Msg02, butSub000100_08.Text, MessageBoxButtons.OK, MessageBoxIcon.Error);
                                    }

                                    //---
                                    //製作多選支援左右鍵切換查詢+修改門區內容 ~ 多選情況 停用 儲存設定 和 套用設置 按鈕事件函數內的 Leave_function呼叫

                                    //Leave_function();

                                    if (m_ALDoorObj.Count == 1)
                                    {
                                        Leave_function();
                                    }

                                    //---製作多選支援左右鍵切換查詢+修改門區內容 ~ 多選情況 停用 儲存設定 和 套用設置 按鈕事件函數內的 Leave_function呼叫
                                }
                                else
                                {
                                    MessageBox.Show(Language.m_StrbutSub000100_08Msg03, butSub000100_08.Text, MessageBoxButtons.OK, MessageBoxIcon.Error);
                                }
                            }
                            else
                            {
                                MessageBox.Show(Language.m_StrbutSub000100_08Msg04, butSub000100_08.Text, MessageBoxButtons.OK, MessageBoxIcon.Error);
                            }
                        }
                        else
                        {
                            MessageBox.Show(Language.m_StrbutSub000100_08Msg05, butSub000100_08.Text, MessageBoxButtons.OK, MessageBoxIcon.Error);
                        }
                    }
                    else
                    {
                        MessageBox.Show(Language.m_StrbutSub000100_08Msg06, butSub000100_08.Text, MessageBoxButtons.OK, MessageBoxIcon.Error);
                    }
                }
                else
                {
                    MessageBox.Show(Language.m_StrbutSub000100_08Msg07, butSub000100_08.Text, MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
            }


        }
        private void butSub000100_06_Click(object sender, EventArgs e)//門區參數儲存
        {
            DoorData2DB();
            //---
            //製作多選支援左右鍵切換查詢+修改門區內容 ~ 多選情況 停用 儲存設定 和 套用設置 按鈕事件函數內的 Leave_function呼叫

            //Leave_function();
            
            if (m_ALDoorObj.Count == 1)
            {
                Leave_function();
            }

            //---製作多選支援左右鍵切換查詢+修改門區內容 ~ 多選情況 停用 儲存設定 和 套用設置 按鈕事件函數內的 Leave_function呼叫
        }
        //--Sub000100_end
        //--Sub000101_start
        private void ckbSub000101_05_CheckedChanged(object sender, EventArgs e)
        {
            txtSub000101_04.Enabled = ckbSub000101_05.Checked;
            txtSub000101_05.Enabled = ckbSub000101_05.Checked;
        }

        private void rdbSub000101_03_CheckedChanged(object sender, EventArgs e)
        {
            rdbSub000101_07.Enabled = rdbSub000101_03.Checked;
            rdbSub000101_08.Enabled = rdbSub000101_03.Checked;
        }

        private void ckbSub000101_40_CheckedChanged(object sender, EventArgs e)
        {
            txtSub000101_07.Enabled = ckbSub000101_40.Checked;
            txtSub000101_08.Enabled = ckbSub000101_40.Checked;
            txtSub000101_09.Enabled = ckbSub000101_40.Checked;
            cmbSub000101_01.Enabled = ckbSub000101_40.Checked;
        }

        private void ckbSub000101_22_CheckedChanged(object sender, EventArgs e)
        {
            rdbSub000101_21.Enabled = ckbSub000101_22.Checked;
            rdbSub000101_22.Enabled = ckbSub000101_22.Checked;
            ckbSub000101_23.Enabled = ckbSub000101_22.Checked;
            ckbSub000101_24.Enabled = ckbSub000101_22.Checked;
            ckbSub000101_25.Enabled = ckbSub000101_22.Checked;
            ckbSub000101_26.Enabled = ckbSub000101_22.Checked;
            ckbSub000101_27.Enabled = ckbSub000101_22.Checked;
            ckbSub000101_28.Enabled = ckbSub000101_22.Checked;
            ckbSub000101_29.Enabled = ckbSub000101_22.Checked;
            ckbSub000101_30.Enabled = ckbSub000101_22.Checked;
            steSub000101_01.Enabled = ckbSub000101_22.Checked;
            steSub000101_02.Enabled = ckbSub000101_22.Checked;
            steSub000101_03.Enabled = ckbSub000101_22.Checked;
            steSub000101_04.Enabled = ckbSub000101_22.Checked;
            steSub000101_05.Enabled = ckbSub000101_22.Checked;
            steSub000101_06.Enabled = ckbSub000101_22.Checked;
            steSub000101_07.Enabled = ckbSub000101_22.Checked;
            steSub000101_08.Enabled = ckbSub000101_22.Checked;
        }

        private void ckbSub000101_31_CheckedChanged(object sender, EventArgs e)
        {
            rdbSub000101_23.Enabled = ckbSub000101_31.Checked;
            rdbSub000101_24.Enabled = ckbSub000101_31.Checked;
            rdbSub000101_25.Enabled = ckbSub000101_31.Checked;
            ckbSub000101_32.Enabled = ckbSub000101_31.Checked;
            ckbSub000101_33.Enabled = ckbSub000101_31.Checked;
            ckbSub000101_34.Enabled = ckbSub000101_31.Checked;
            ckbSub000101_35.Enabled = ckbSub000101_31.Checked;
            ckbSub000101_36.Enabled = ckbSub000101_31.Checked;
            ckbSub000101_37.Enabled = ckbSub000101_31.Checked;
            ckbSub000101_38.Enabled = ckbSub000101_31.Checked;
            ckbSub000101_39.Enabled = ckbSub000101_31.Checked;
            steSub000101_09.Enabled = ckbSub000101_31.Checked;
            steSub000101_10.Enabled = ckbSub000101_31.Checked;
            steSub000101_11.Enabled = ckbSub000101_31.Checked;
            steSub000101_12.Enabled = ckbSub000101_31.Checked;
            steSub000101_13.Enabled = ckbSub000101_31.Checked;
            steSub000101_14.Enabled = ckbSub000101_31.Checked;
            steSub000101_15.Enabled = ckbSub000101_31.Checked;
            steSub000101_16.Enabled = ckbSub000101_31.Checked;
        }
        //--Sub000101_end
        //--Sub0000_start
        private void dgvSub0000_01_DoubleClick(object sender, EventArgs e)//at 2017/09/15
        {
            butSub0000_01.PerformClick();
        }

        //---
        public void get_show_Controllers_at20180309()//取的控制器列表
        {
            try
            {
                //--
                //dgvSub000001_01.ReadOnly = true;//唯讀 不可更改
                dgvSub0000_01.RowHeadersVisible = false;//DataGridView 最前面指示選取列所在位置的箭頭欄位
                dgvSub0000_01.Rows[0].Selected = false;//取消DataGridView的默認選取(選中)Cell 使其不反藍
                dgvSub0000_01.AllowUserToAddRows = false;//是否允許使用者新增資料
                dgvSub0000_01.AllowUserToDeleteRows = false;//是否允許使用者刪除資料
                dgvSub0000_01.AllowUserToOrderColumns = false;//是否允許使用者調整欄位位置
                //所有表格欄位寬度全部變成可調 dgvSub0000_01.AllowUserToResizeColumns = false;//是否允許使用者改變欄寬
                dgvSub0000_01.AllowUserToResizeRows = false;//是否允許使用者改變行高
                dgvSub0000_01.Columns[1].ReadOnly = true;//單一欄位禁止編輯
                dgvSub0000_01.Columns[2].ReadOnly = true;//單一欄位禁止編輯
                dgvSub0000_01.Columns[3].ReadOnly = true;//單一欄位禁止編輯
                dgvSub0000_01.Columns[4].ReadOnly = true;//單一欄位禁止編輯
                dgvSub0000_01.Columns[5].ReadOnly = true;//單一欄位禁止編輯
                dgvSub0000_01.Columns[6].ReadOnly = true;//單一欄位禁止編輯
                dgvSub0000_01.AllowUserToAddRows = false;//刪除空白列
                dgvSub0000_01.SelectionMode = System.Windows.Forms.DataGridViewSelectionMode.FullRowSelect;//整列選取
                //--

                do
                {
                    for (int i = 0; i < dgvSub0000_01.Rows.Count; i++)
                    {
                        DataGridViewRow r1 = this.dgvSub0000_01.Rows[i];//取得DataGridView整列資料
                        this.dgvSub0000_01.Rows.Remove(r1);//DataGridView刪除整列
                    }
                } while (dgvSub0000_01.Rows.Count > 0);

            }
            catch
            {
            }

            bool blnHaveSy_dm = false;//無連接Sy_dm 
            if (txtSys_08.SelectedIndex == 0)//選擇抓取SYDM at 2017/09/18 21:13
            {
                //---
                //SYDM和SYCG API呼叫並存實現
                //HW_Net_API.setAPIDomainURL(txtSys_01.Text, txtSys_07.Text);
                if (!m_changeSYCGMode)//SYDM
                {
                    HW_Net_API.setAPIDomainURL(txtSys_01.Text, txtSys_07.Text);
                }
                else//SYCG
                {
                    HW_Net_API.SYCG_setSYCGDomainURL();
                }
                //---SYDM和SYCG API呼叫並存實現

                //---
                //SYDM和SYCG API呼叫並存實現
                if (!m_changeSYCGMode)//SYDM
                {
                    m_blnAPI = HW_Net_API.getController_Connection();
                }
                else//SYCG
                {
                    m_blnAPI = HW_Net_API.SYCG_getSYDMList();
                    if (m_blnAPI)
                    {
                        HW_Net_API.m_Controller_Connection.controllers.Clear();
                        for (int l = 0; l < m_Sydms.sydms.Count; l++)
                        {
                            m_blnAPI = HW_Net_API.SYCG_callSYDMCommand("SYDM_GET_CONTROLLER_CONNECTION", "", m_Sydms.sydms[l].identifier.ToString());
                        }
                    }
                }
                //---SYDM和SYCG API呼叫並存實現	
                if (m_blnAPI)//if (HW_Net_API.getController_Connection())//實際聯結機器，太慢~if (HW_Net_API.getController())
                {
                    blnHaveSy_dm = true;//有連接Sy_dm 
                }
            }
            /*
            String SQL = @"SELECT A.id AS ID,A.sn AS SN,C.model_name AS Model,A.name AS Name,A.alias AS Alias,B.connetction_enabled AS State,B.connetction_address AS IP
                        FROM controller AS A,controller_dm AS B,models AS C WHERE (A.id=B.id) AND (B.sn=B.sn) AND (A.model=B.model) AND (A.model=C.model)" + m_SQLcondition01 + m_SQLcondition02 + m_SQLcondition03 + m_SQLcondition04 + m_SQLcondition05 + m_SQLcondition06 + ";";
            */
            //*
            String SQL = "";
            if (!m_changeSYCGMode)//SYDM
            {
                SQL = @"SELECT c.sydm_id AS sydm_id,c.id AS id,c.name AS name,c.alias AS alias,c.sn AS sn,m.model_name AS model,c_e.connetction_enabled AS enabled,c_e.connetction_address AS ip,c_e.port AS port,c_e.connetction_mode AS mode
                           FROM controller AS c,models AS m,controller_extend AS c_e
                           WHERE (c.model=m.model) AND (c.sn=c_e.controller_sn) AND (c_e.door_number=m.door_number)" + m_SQLcondition01 + m_SQLcondition02 + m_SQLcondition03 + m_SQLcondition04 + m_SQLcondition05 + m_SQLcondition06 + " GROUP BY c.id ORDER BY c.id;";//因為要區分4/12門控制器所以再次修改 at 2017/07/28 //修正控制器列表增加防堵沒給SN重複顯示 -> GROUP BY c.id
            }
            else//SYCG
            {
                SQL = @"SELECT s.name AS sydm_name,c.sydm_id AS sydm_id,c.id AS id,c.name AS name,c.alias AS alias,c.sn AS sn,m.model_name AS model,c_e.connetction_enabled AS enabled,c_e.connetction_address AS ip,c_e.port AS port,c_e.connetction_mode AS mode
                           FROM controller AS c,models AS m,controller_extend AS c_e,sydm AS s
                           WHERE (s.id = c.sydm_id) AND (c.model=m.model) AND (c.sn=c_e.controller_sn) AND (c_e.door_number=m.door_number)" + m_SQLcondition01 + m_SQLcondition02 + m_SQLcondition03 + m_SQLcondition04 + m_SQLcondition05 + m_SQLcondition06 + " GROUP BY c.id ORDER BY c.sydm_id,c.id;";//因為要區分4/12門控制器所以再次修改 at 2017/07/28 //修正控制器列表增加防堵沒給SN重複顯示 -> GROUP BY c.id
            }
            //*/
            MySqlDataReader DataReader = MySQL.GetDataReader(SQL);
            while (DataReader.Read())
            {
                String StrID = DataReader["id"].ToString();
                String StrSN = DataReader["sn"].ToString();//Convert.ToString(Int32.Parse(DataReader["sn"].ToString()), 16);
                String StrModel = DataReader["model"].ToString();//"0x" + Convert.ToString(Int32.Parse(DataReader["Model"].ToString()), 16);
                String StrName = DataReader["name"].ToString();
                String StrAlias = DataReader["alias"].ToString();
                String StrPort = DataReader["port"].ToString();
                String StrMode = DataReader["mode"].ToString();
                m_intSYDM_id = Convert.ToInt32(DataReader["sydm_id"].ToString());//SYCG模式下-建立/暫存 當下要操作的SYDM ID
                
                String StrSydmName = "";
                if (m_changeSYCGMode)
                {
                    StrSydmName = DataReader["sydm_name"].ToString();
                }
                
                /*
                for (int i = 0; i < HW_Net_API.m_ALHW_ID.Count; i++)
                {
                    String Strbuf = HW_Net_API.m_ALHW_ID[i].ToString().ToLower();
                    if (StrModel == Strbuf)
                    {
                        StrModel = HW_Net_API.m_ALHW_Name[i].ToString();
                    }
                }
                */
                if (StrAlias != "")
                {
                    StrName += "( " + StrAlias + " )";
                }

                if (StrSydmName != "")
                {
                    StrName = StrSydmName + " - " + StrName;
                }

                String StrState = DataReader["enabled"].ToString();
                if (StrState == "0")
                {
                    StrState = "Disable";
                }
                else
                {
                    StrState = "Enable";
                }
                String StrIP = DataReader["ip"].ToString();//HW_Net_API.long2ip(Convert.ToInt32(DataReader["IP"].ToString()), true);
                if (!blnHaveSy_dm)
                {
                    this.dgvSub0000_01.Rows.Add(false, StrID, StrName, StrModel, StrSN, StrIP, StrState, StrPort, StrMode, m_Img_n);//無連接Sy_dm 
                }
                else
                {
                    int intsy_dm_Controller_id = -1;
                    intsy_dm_Controller_id = Getsy_dm_Controller_id(StrMode, StrIP, StrPort);//修正Getsy_dm_Controller_id 函數和相關呼叫點-intsy_dm_Controller_id = Getsy_dm_Controller_id(StrSN, StrIP, StrPort);
                    if (intsy_dm_Controller_id > 0)
                    {
                        //有找到

			            //---
			            //SYDM和SYCG API呼叫並存實現
			            if(!m_changeSYCGMode)//SYDM
			            {
                            m_blnAPI = HW_Net_API.getController_Status(intsy_dm_Controller_id);
			            }
			            else//SYCG
			            {
				            String StrGCS_buf = "\"identifier\":" + intsy_dm_Controller_id;			
				            m_blnAPI = HW_Net_API.SYCG_callSYDMCommand("SYDM_GET_CONTROLLER_STATUS", StrGCS_buf, m_intSYDM_id.ToString());
			            }
			            //---SYDM和SYCG API呼叫並存實現
			            if (m_blnAPI)//if (HW_Net_API.getController_Status(intsy_dm_Controller_id))
                        {
                            if (HW_Net_API.m_Controller_Status.controllers != null)
                            {
                                if (HW_Net_API.m_Controller_Status.controllers[0].status.is_connected > 0)
                                {
                                    this.dgvSub0000_01.Rows.Add(false, StrID, StrName, StrModel, StrSN, StrIP, StrState, StrPort, StrMode, m_Img_g);//找到剛才控制器，且有連線
                                }
                                else
                                {
                                    this.dgvSub0000_01.Rows.Add(false, StrID, StrName, StrModel, StrSN, StrIP, StrState, StrPort, StrMode, m_Img_r);//找到剛才控制器，但未連線
                                }
                            }
                            else
                            {
                                this.dgvSub0000_01.Rows.Add(false, StrID, StrName, StrModel, StrSN, StrIP, StrState, StrPort, StrMode, m_Img_r);//找到剛才控制器，但未連線
                            }
                        }
                        else
                        {
                            this.dgvSub0000_01.Rows.Add(false, StrID, StrName, StrModel, StrSN, StrIP, StrState, StrPort, StrMode, m_Img_n);//找到剛才新增控制器，但未連線
                        }
                    }
                    else
                    {
                        //沒找到
                        this.dgvSub0000_01.Rows.Add(false, StrID, StrName, StrModel, StrSN, StrIP, StrState, StrPort, StrMode, m_Img_n);//Sy_dm 內無資料
                    }
                }
            }
            DataReader.Close();
        }

        public void get_show_Controllers(bool blnnotrefresh=true)//取的控制器列表
        {
            if (!(m_blnget_show_Controllers && blnnotrefresh))
            {
                //---
                //控制器列表元件清空
                cleandgvSub0000_01();
                //---控制器列表元件清空

                //---
                //從DB取的要顯示在列表原件上的資料儲存在ArrayList中
                getDBControllerShowData();
                //---

                //---
                //顯示假資料
                for (int i = 0; i < m_ALControllerShowData.Count; i++)
                {
                    this.dgvSub0000_01.Rows.Add(false, ((ControllerShowData)m_ALControllerShowData[i]).m_StrID, ((ControllerShowData)m_ALControllerShowData[i]).m_StrName, ((ControllerShowData)m_ALControllerShowData[i]).m_StrModel, ((ControllerShowData)m_ALControllerShowData[i]).m_StrSN, ((ControllerShowData)m_ALControllerShowData[i]).m_StrIP, ((ControllerShowData)m_ALControllerShowData[i]).m_StrPort, ((ControllerShowData)m_ALControllerShowData[i]).m_StrState, ((ControllerShowData)m_ALControllerShowData[i]).m_StrMode, m_Img_n);//Sy_dm 內無資料 //修正控制器列表資訊排列順序
                }
                //---顯示假資料

                if (txtSys_08.SelectedIndex == 0)//修正BUG-get_show_Controllers必須支援系統頁面的操作模式選項
                {
                    //---
                    //API抓取控制器狀態

                    //---
                    //SYDM和SYCG API呼叫並存實現
                    //HW_Net_API.setAPIDomainURL(txtSys_01.Text, txtSys_07.Text);
                    if (!m_changeSYCGMode)//SYDM
                    {
                        HW_Net_API.setAPIDomainURL(txtSys_01.Text, txtSys_07.Text);
                    }
                    else//SYCG
                    {
                        HW_Net_API.SYCG_setSYCGDomainURL();
                    }
                    //---SYDM和SYCG API呼叫並存實現

                    Animation.createThreadAnimation(butSub0000_11.Text, Animation.Thread_getControllerStatus);

                    //---API抓取控制器狀態
                }
                m_blnget_show_Controllers = true;
            }

            //---
            //控制器列表元件清空
            cleandgvSub0000_01();
            //---控制器列表元件清空

            //---
            //顯示真實資料
            for (int i = 0; i < m_ALControllerShowData.Count; i++)
            {
                switch (((ControllerShowData)m_ALControllerShowData[i]).m_intImage)
                {
                    case 0:
                        this.dgvSub0000_01.Rows.Add(false, ((ControllerShowData)m_ALControllerShowData[i]).m_StrID, ((ControllerShowData)m_ALControllerShowData[i]).m_StrName, ((ControllerShowData)m_ALControllerShowData[i]).m_StrModel, ((ControllerShowData)m_ALControllerShowData[i]).m_StrSN, ((ControllerShowData)m_ALControllerShowData[i]).m_StrIP, ((ControllerShowData)m_ALControllerShowData[i]).m_StrPort, ((ControllerShowData)m_ALControllerShowData[i]).m_StrState, ((ControllerShowData)m_ALControllerShowData[i]).m_StrMode, m_Img_n);//Sy_dm 內無資料 //修正控制器列表資訊排列順序
                        break;
                    case 1:
                        this.dgvSub0000_01.Rows.Add(false, ((ControllerShowData)m_ALControllerShowData[i]).m_StrID, ((ControllerShowData)m_ALControllerShowData[i]).m_StrName, ((ControllerShowData)m_ALControllerShowData[i]).m_StrModel, ((ControllerShowData)m_ALControllerShowData[i]).m_StrSN, ((ControllerShowData)m_ALControllerShowData[i]).m_StrIP, ((ControllerShowData)m_ALControllerShowData[i]).m_StrPort, ((ControllerShowData)m_ALControllerShowData[i]).m_StrState, ((ControllerShowData)m_ALControllerShowData[i]).m_StrMode, m_Img_g);//修正控制器列表資訊排列順序
                        break;
                    case 2:
                        this.dgvSub0000_01.Rows.Add(false, ((ControllerShowData)m_ALControllerShowData[i]).m_StrID, ((ControllerShowData)m_ALControllerShowData[i]).m_StrName, ((ControllerShowData)m_ALControllerShowData[i]).m_StrModel, ((ControllerShowData)m_ALControllerShowData[i]).m_StrSN, ((ControllerShowData)m_ALControllerShowData[i]).m_StrIP, ((ControllerShowData)m_ALControllerShowData[i]).m_StrPort, ((ControllerShowData)m_ALControllerShowData[i]).m_StrState, ((ControllerShowData)m_ALControllerShowData[i]).m_StrMode, m_Img_r);//修正控制器列表資訊排列順序
                        break;
                }
            }
            //---顯示真實資料
        }

        private ArrayList m_ALSub000001_Old_Data = new ArrayList();
        private void butSub0000_01_Click(object sender, EventArgs e)//顯示編修控制器頁面
        {
            String SQL = "";

            m_TPOld = m_tabMain.SelectedTab;//--2017/02/22 製作返回按鈕功能
            TabPage_Push();//m_StackTPOld.Push(m_TPOld);//--2017/02/22 製作返回按鈕功能

            //---
            //控制器UI多選編輯實作 ~ 撰寫儲存查詢相關變數
            /*
            m_intcontroller_sn = m_intdgvSub0000_01_SN;//新增模式 所以沒有sn 2017/06/30
            //--
            //未知控制器禁止編輯
            bool blncheck = false;
            SQL = String.Format("SELECT id FROM controller WHERE (state>-1) AND (sn={0})", m_intcontroller_sn);
            MySqlDataReader checkReader = MySQL.GetDataReader(SQL);
            while (checkReader.Read())
            {
                blncheck = true;
            }
            checkReader.Close();
            if (!blncheck)
            {
                MessageBox.Show(Language.m_StrbutSub0000_01Msg00, butSub0000_01.Text, MessageBoxButtons.OK, MessageBoxIcon.Error);//MessageBox全部支援多國語系
                return;
            }
            //--
            */
            initSelectControllerArray();
            if (m_ALControllerObj.Count > 1)
            {
                m_intdgvSub0000_01_SN = Int32.Parse(m_ALControllerObj[m_intControllerIndex].ToString());
                m_intcontroller_sn = m_intdgvSub0000_01_SN;
            }
            else
            {
                m_intcontroller_sn = m_intdgvSub0000_01_SN;//新增模式 所以沒有sn 2017/06/30

                bool blncheck = false;
                SQL = String.Format("SELECT id FROM controller WHERE (state>-1) AND (sn={0})", m_intcontroller_sn);
                MySqlDataReader checkReader = MySQL.GetDataReader(SQL);
                while (checkReader.Read())
                {
                    blncheck = true;
                }
                checkReader.Close();
                if (!blncheck)
                {
                    MessageBox.Show(Language.m_StrbutSub0000_01Msg00, butSub0000_01.Text, MessageBoxButtons.OK, MessageBoxIcon.Error);//MessageBox全部支援多國語系
                    return;
                }
            }
            //---控制器UI多選編輯實作 ~ 撰寫儲存查詢相關變數
            
            //---
            //控制器UI多選編輯實作 ~ 把顯示控制器參數獨立成函數[setSub000001UI]
            
            /*
            m_intdgvSub0000_01_SN = -1;
            initSub000001UI(true,""+m_intcontroller_sn);//要把假日列表，依控制器獨立切開 at 2017/08/16 -- initSub000001UI();//用來切換子元件顯示-2017/03/03
            cmbSub000001_01.Enabled = false;//控制器在SERVER mode 時 SN 自動填入 日+時+分+秒
            //--
            //取出對應的controller 表的資料
            String Strsn, Strmodel, Strname, Stralias, Strdoor_number;
            SQL = String.Format("SELECT c.sydm_id AS sydm_id,c.sn AS sn,c.model AS model,c.name AS name,c.alias AS alias,c_e.door_number AS door_number FROM controller AS c,controller_extend AS c_e WHERE c.sn=c_e.controller_sn AND c.sn={0};", m_intcontroller_sn);//為了新增可以區分4/12門控制器 所以要修改 at 2017/07/28
            MySqlDataReader controllerReader = MySQL.GetDataReader(SQL);
            while (controllerReader.Read())
            {
                m_intdgvSub0000_01_SN = m_intcontroller_sn;
                Strsn = controllerReader["sn"].ToString();
                Strmodel = controllerReader["model"].ToString();
                Strname = controllerReader["name"].ToString();
                Stralias = controllerReader["alias"].ToString();
                Strdoor_number = controllerReader["door_number"].ToString();//為了新增可以區分4/12門控制器 所以要修改 at 2017/07/28
                txtSub000001_01.Text = Strname;
                txtSub000001_02.Text = Stralias;
                for (int i = 0; i < HW_Net_API.m_ALHW_ID.Count; i++)
                {
                    if ( (Convert.ToInt32(HW_Net_API.m_ALHW_ID[i].ToString(), 16) == Convert.ToInt32(Strmodel)) && (Strdoor_number == HW_Net_API.m_ALHW_DoorNumber[i].ToString()) )//為了新增可以區分4/12門控制器 所以要修改 at 2017/07/28
                    {
                        cmbSub000001_03.SelectedIndex = i;
                        break;
                    }
                }
                labSub000001_09.Text = Strsn;

                //---
                //SYDM 選擇元件在指定控制器時顯示對應值
                cmbSub000001_04.Enabled = false;
                cmbSub000001_04.SelectedIndex = -1;//防止SYDM被刪
                if (controllerReader["sydm_id"].ToString() == "0")
                {
                    cmbSub000001_04.SelectedIndex = -1;
                }
                else
                {
                    for (int i = 0; i < m_ALSYDM_ID.Count; i++)
                    {
                        if (((String)m_ALSYDM_ID[i]) == controllerReader["sydm_id"].ToString())
                        {
                            cmbSub000001_04.SelectedIndex = i;
                        }
                    }
                }
                //---

                m_intSYDM_id = Convert.ToInt32(controllerReader["sydm_id"].ToString());//SYCG模式下-建立/暫存 當下要操作的SYDM ID
                
                break;
            }
            controllerReader.Close();
            if (m_intdgvSub0000_01_SN < 0)
            {
                return;//沒有選擇修改項目，防呆
            }
            //--
            //--
            //取出對應的controller_extend 表的資料
            String Strconnetction_address, Strconnetction_enabled, Strconnetction_mode, Strapb_enable, Strapb_mode, Strapb_group, Strapb_level_list, Strapb_reset_timestamp_list, Strab_door_enabled, Strab_door_level, Strab_door_timeout_second, Strab_door_reset_time_second, Strsame_card_interval_time_second;
            Strab_door_enabled="";
            SQL = String.Format("SELECT connetction_address,connetction_enabled,connetction_mode,apb_enable,apb_mode,apb_group,apb_level_list,apb_reset_timestamp_list,ab_door_enabled,ab_door_level,ab_door_timeout_second,ab_door_reset_time_second,same_card_interval_time_second FROM controller_extend WHERE controller_sn={0};", m_intcontroller_sn);
            MySqlDataReader controller_extendReader = MySQL.GetDataReader(SQL);
            while (controller_extendReader.Read())
            {
                Strsame_card_interval_time_second = controller_extendReader["same_card_interval_time_second"].ToString();//add 2017/0822
                Strconnetction_address = controller_extendReader["connetction_address"].ToString();
                Strconnetction_enabled = controller_extendReader["connetction_enabled"].ToString();
                Strconnetction_mode = controller_extendReader["connetction_mode"].ToString();
                Strapb_enable = controller_extendReader["apb_enable"].ToString();
                Strapb_mode = controller_extendReader["apb_mode"].ToString();
                Strapb_group = controller_extendReader["apb_group"].ToString();
                Strapb_level_list = controller_extendReader["apb_level_list"].ToString();
                Strapb_reset_timestamp_list = controller_extendReader["apb_reset_timestamp_list"].ToString();
                Strab_door_enabled = controller_extendReader["ab_door_enabled"].ToString();
                Strab_door_level = controller_extendReader["ab_door_level"].ToString();
                Strab_door_timeout_second = controller_extendReader["ab_door_timeout_second"].ToString();
                Strab_door_reset_time_second = controller_extendReader["ab_door_reset_time_second"].ToString();
                txtSub000001_04.Text = Strconnetction_address;
                txtSub000001_07.Text = Strsame_card_interval_time_second;//add 2017/0822
                if(Strconnetction_enabled != "0")
                {
	                rdbSub000001_01.Checked = true;
                }
                else
                {
	                rdbSub000001_02.Checked = true;
                }
                cmbSub000001_01.SelectedIndex=Convert.ToInt32(Strconnetction_mode);
                if(Strapb_enable != "0")
                {
	                ckbSub000001_01.Checked = true;
                }
                else
                {
	                ckbSub000001_01.Checked = false;
                }

                rdbSub000001_03.Checked = false;
                rdbSub000001_04.Checked = false;
                if(Strapb_mode == "1")
                {
	                rdbSub000001_03.Checked = true;
                }
                else if (Strapb_mode == "2")
                {
	                rdbSub000001_04.Checked = true;
                }

                //Strapb_group
                //Strapb_level_list
                //Strapb_reset_timestamp_list
                txtSub000001_19.Value = Convert.ToInt32(Strab_door_level);
                if(Strab_door_enabled != "0")
                {
	                ckbSub000001_02.Checked = true;

                    AB12LEVEL4Sub000001_01.jlNumEdit1.Value = Convert.ToInt32(Strab_door_timeout_second);
                    AB12LEVEL4Sub000001_01.jlNumEdit2.Value = Convert.ToInt32(Strab_door_reset_time_second);

                    AB12LEVEL3Sub000001_01.jlNumEdit1.Value = Convert.ToInt32(Strab_door_timeout_second);
                    AB12LEVEL3Sub000001_01.jlNumEdit2.Value = Convert.ToInt32(Strab_door_reset_time_second);

                    AB12LEVEL2Sub000001_01.jlNumEdit1.Value = Convert.ToInt32(Strab_door_timeout_second);
                    AB12LEVEL2Sub000001_01.jlNumEdit2.Value = Convert.ToInt32(Strab_door_reset_time_second);

                    AB4LEVEL4Sub000001_01.jlNumEdit1.Value = Convert.ToInt32(Strab_door_timeout_second);
                    AB4LEVEL4Sub000001_01.jlNumEdit2.Value = Convert.ToInt32(Strab_door_reset_time_second);

                    AB4LEVEL3Sub000001_01.jlNumEdit1.Value = Convert.ToInt32(Strab_door_timeout_second);
                    AB4LEVEL3Sub000001_01.jlNumEdit2.Value = Convert.ToInt32(Strab_door_reset_time_second);

                    AB4LEVEL2Sub000001_01.jlNumEdit1.Value = Convert.ToInt32(Strab_door_timeout_second);
                    AB4LEVEL2Sub000001_01.jlNumEdit2.Value = Convert.ToInt32(Strab_door_reset_time_second);
                }
                else
                {
	                ckbSub000001_02.Checked = false;
                }
                ChangeSub000001UI(true);//觸發置換下方子元件

                break;
            }
            controller_extendReader.Close();
            //--
            //--
            //取出對應的door 表的資料
            m_ALDoors.Clear();
            m_ALDoor_id.Clear();
            SQL = String.Format("SELECT id,name FROM door WHERE controller_id={0} ORDER BY id ASC;", m_intcontroller_sn);
            MySqlDataReader doorReader = MySQL.GetDataReader(SQL);
            while (doorReader.Read())
            {
                m_ALDoor_id.Add(doorReader["id"].ToString());
                m_ALDoors.Add(doorReader["name"].ToString());
            }
            doorReader.Close();
            if (Strab_door_enabled == "0")//一般
            {
                switch (m_ALDoors.Count)
                {
                    //--
                    //add 2017/10/19
                    case 128:
                        egsSub000001_01.m_ALAllName.Clear();
                        for (int i = 0; i < m_ALDoors.Count; i++)
                        {
                            egsSub000001_01.m_ALAllName.Add(m_ALDoors[i].ToString());
                        }
                        egsSub000001_01.SetAllName();
                        break;
                    //--
                    case 12:
                        Door12Sub000001_01.ckbDoor12_01.Checked = true;
                        Door12Sub000001_01.ckbDoor12_02.Checked = true;
                        Door12Sub000001_01.ckbDoor12_03.Checked = true;
                        Door12Sub000001_01.ckbDoor12_04.Checked = true;
                        Door12Sub000001_01.ckbDoor12_05.Checked = true;
                        Door12Sub000001_01.ckbDoor12_06.Checked = true;
                        Door12Sub000001_01.ckbDoor12_07.Checked = true;
                        Door12Sub000001_01.ckbDoor12_08.Checked = true;
                        Door12Sub000001_01.ckbDoor12_09.Checked = true;
                        Door12Sub000001_01.ckbDoor12_10.Checked = true;
                        Door12Sub000001_01.ckbDoor12_11.Checked = true;
                        Door12Sub000001_01.ckbDoor12_12.Checked = true;
                        Door12Sub000001_01.txtDoor12_01.Text = m_ALDoors[00].ToString();
                        Door12Sub000001_01.txtDoor12_02.Text = m_ALDoors[01].ToString();
                        Door12Sub000001_01.txtDoor12_03.Text = m_ALDoors[02].ToString();
                        Door12Sub000001_01.txtDoor12_04.Text = m_ALDoors[03].ToString();
                        Door12Sub000001_01.txtDoor12_05.Text = m_ALDoors[04].ToString();
                        Door12Sub000001_01.txtDoor12_06.Text = m_ALDoors[05].ToString();
                        Door12Sub000001_01.txtDoor12_07.Text = m_ALDoors[06].ToString();
                        Door12Sub000001_01.txtDoor12_08.Text = m_ALDoors[07].ToString();
                        Door12Sub000001_01.txtDoor12_09.Text = m_ALDoors[08].ToString();
                        Door12Sub000001_01.txtDoor12_10.Text = m_ALDoors[09].ToString();
                        Door12Sub000001_01.txtDoor12_11.Text = m_ALDoors[10].ToString();
                        Door12Sub000001_01.txtDoor12_12.Text = m_ALDoors[11].ToString();
                        break;
                    case 11:
                        Door12Sub000001_01.ckbDoor12_01.Checked = true;
                        Door12Sub000001_01.ckbDoor12_02.Checked = true;
                        Door12Sub000001_01.ckbDoor12_03.Checked = true;
                        Door12Sub000001_01.ckbDoor12_04.Checked = true;
                        Door12Sub000001_01.ckbDoor12_05.Checked = true;
                        Door12Sub000001_01.ckbDoor12_06.Checked = true;
                        Door12Sub000001_01.ckbDoor12_07.Checked = true;
                        Door12Sub000001_01.ckbDoor12_08.Checked = true;
                        Door12Sub000001_01.ckbDoor12_09.Checked = true;
                        Door12Sub000001_01.ckbDoor12_10.Checked = true;
                        Door12Sub000001_01.ckbDoor12_11.Checked = true;
                        Door12Sub000001_01.txtDoor12_01.Text = m_ALDoors[00].ToString();
                        Door12Sub000001_01.txtDoor12_02.Text = m_ALDoors[01].ToString();
                        Door12Sub000001_01.txtDoor12_03.Text = m_ALDoors[02].ToString();
                        Door12Sub000001_01.txtDoor12_04.Text = m_ALDoors[03].ToString();
                        Door12Sub000001_01.txtDoor12_05.Text = m_ALDoors[04].ToString();
                        Door12Sub000001_01.txtDoor12_06.Text = m_ALDoors[05].ToString();
                        Door12Sub000001_01.txtDoor12_07.Text = m_ALDoors[06].ToString();
                        Door12Sub000001_01.txtDoor12_08.Text = m_ALDoors[07].ToString();
                        Door12Sub000001_01.txtDoor12_09.Text = m_ALDoors[08].ToString();
                        Door12Sub000001_01.txtDoor12_10.Text = m_ALDoors[09].ToString();
                        Door12Sub000001_01.txtDoor12_11.Text = m_ALDoors[10].ToString();
                        break;
                    case 10:
                        Door12Sub000001_01.ckbDoor12_01.Checked = true;
                        Door12Sub000001_01.ckbDoor12_02.Checked = true;
                        Door12Sub000001_01.ckbDoor12_03.Checked = true;
                        Door12Sub000001_01.ckbDoor12_04.Checked = true;
                        Door12Sub000001_01.ckbDoor12_05.Checked = true;
                        Door12Sub000001_01.ckbDoor12_06.Checked = true;
                        Door12Sub000001_01.ckbDoor12_07.Checked = true;
                        Door12Sub000001_01.ckbDoor12_08.Checked = true;
                        Door12Sub000001_01.ckbDoor12_09.Checked = true;
                        Door12Sub000001_01.ckbDoor12_10.Checked = true;
                        Door12Sub000001_01.txtDoor12_01.Text = m_ALDoors[00].ToString();
                        Door12Sub000001_01.txtDoor12_02.Text = m_ALDoors[01].ToString();
                        Door12Sub000001_01.txtDoor12_03.Text = m_ALDoors[02].ToString();
                        Door12Sub000001_01.txtDoor12_04.Text = m_ALDoors[03].ToString();
                        Door12Sub000001_01.txtDoor12_05.Text = m_ALDoors[04].ToString();
                        Door12Sub000001_01.txtDoor12_06.Text = m_ALDoors[05].ToString();
                        Door12Sub000001_01.txtDoor12_07.Text = m_ALDoors[06].ToString();
                        Door12Sub000001_01.txtDoor12_08.Text = m_ALDoors[07].ToString();
                        Door12Sub000001_01.txtDoor12_09.Text = m_ALDoors[08].ToString();
                        Door12Sub000001_01.txtDoor12_10.Text = m_ALDoors[09].ToString();
                        break;
                    case 09:
                        Door12Sub000001_01.ckbDoor12_01.Checked = true;
                        Door12Sub000001_01.ckbDoor12_02.Checked = true;
                        Door12Sub000001_01.ckbDoor12_03.Checked = true;
                        Door12Sub000001_01.ckbDoor12_04.Checked = true;
                        Door12Sub000001_01.ckbDoor12_05.Checked = true;
                        Door12Sub000001_01.ckbDoor12_06.Checked = true;
                        Door12Sub000001_01.ckbDoor12_07.Checked = true;
                        Door12Sub000001_01.ckbDoor12_08.Checked = true;
                        Door12Sub000001_01.ckbDoor12_09.Checked = true;
                        Door12Sub000001_01.txtDoor12_01.Text = m_ALDoors[00].ToString();
                        Door12Sub000001_01.txtDoor12_02.Text = m_ALDoors[01].ToString();
                        Door12Sub000001_01.txtDoor12_03.Text = m_ALDoors[02].ToString();
                        Door12Sub000001_01.txtDoor12_04.Text = m_ALDoors[03].ToString();
                        Door12Sub000001_01.txtDoor12_05.Text = m_ALDoors[04].ToString();
                        Door12Sub000001_01.txtDoor12_06.Text = m_ALDoors[05].ToString();
                        Door12Sub000001_01.txtDoor12_07.Text = m_ALDoors[06].ToString();
                        Door12Sub000001_01.txtDoor12_08.Text = m_ALDoors[07].ToString();
                        Door12Sub000001_01.txtDoor12_09.Text = m_ALDoors[08].ToString();
                        break;
                    case 08:
                        Door12Sub000001_01.ckbDoor12_01.Checked = true;
                        Door12Sub000001_01.ckbDoor12_02.Checked = true;
                        Door12Sub000001_01.ckbDoor12_03.Checked = true;
                        Door12Sub000001_01.ckbDoor12_04.Checked = true;
                        Door12Sub000001_01.ckbDoor12_05.Checked = true;
                        Door12Sub000001_01.ckbDoor12_06.Checked = true;
                        Door12Sub000001_01.ckbDoor12_07.Checked = true;
                        Door12Sub000001_01.ckbDoor12_08.Checked = true;
                        Door12Sub000001_01.txtDoor12_01.Text = m_ALDoors[00].ToString();
                        Door12Sub000001_01.txtDoor12_02.Text = m_ALDoors[01].ToString();
                        Door12Sub000001_01.txtDoor12_03.Text = m_ALDoors[02].ToString();
                        Door12Sub000001_01.txtDoor12_04.Text = m_ALDoors[03].ToString();
                        Door12Sub000001_01.txtDoor12_05.Text = m_ALDoors[04].ToString();
                        Door12Sub000001_01.txtDoor12_06.Text = m_ALDoors[05].ToString();
                        Door12Sub000001_01.txtDoor12_07.Text = m_ALDoors[06].ToString();
                        Door12Sub000001_01.txtDoor12_08.Text = m_ALDoors[07].ToString();
                        break;
                    case 07:
                        Door12Sub000001_01.ckbDoor12_01.Checked = true;
                        Door12Sub000001_01.ckbDoor12_02.Checked = true;
                        Door12Sub000001_01.ckbDoor12_03.Checked = true;
                        Door12Sub000001_01.ckbDoor12_04.Checked = true;
                        Door12Sub000001_01.ckbDoor12_05.Checked = true;
                        Door12Sub000001_01.ckbDoor12_06.Checked = true;
                        Door12Sub000001_01.ckbDoor12_07.Checked = true;
                        Door12Sub000001_01.txtDoor12_01.Text = m_ALDoors[00].ToString();
                        Door12Sub000001_01.txtDoor12_02.Text = m_ALDoors[01].ToString();
                        Door12Sub000001_01.txtDoor12_03.Text = m_ALDoors[02].ToString();
                        Door12Sub000001_01.txtDoor12_04.Text = m_ALDoors[03].ToString();
                        Door12Sub000001_01.txtDoor12_05.Text = m_ALDoors[04].ToString();
                        Door12Sub000001_01.txtDoor12_06.Text = m_ALDoors[05].ToString();
                        Door12Sub000001_01.txtDoor12_07.Text = m_ALDoors[06].ToString();
                        break;
                    case 06:
                        Door12Sub000001_01.ckbDoor12_01.Checked = true;
                        Door12Sub000001_01.ckbDoor12_02.Checked = true;
                        Door12Sub000001_01.ckbDoor12_03.Checked = true;
                        Door12Sub000001_01.ckbDoor12_04.Checked = true;
                        Door12Sub000001_01.ckbDoor12_05.Checked = true;
                        Door12Sub000001_01.ckbDoor12_06.Checked = true;
                        Door12Sub000001_01.txtDoor12_01.Text = m_ALDoors[00].ToString();
                        Door12Sub000001_01.txtDoor12_02.Text = m_ALDoors[01].ToString();
                        Door12Sub000001_01.txtDoor12_03.Text = m_ALDoors[02].ToString();
                        Door12Sub000001_01.txtDoor12_04.Text = m_ALDoors[03].ToString();
                        Door12Sub000001_01.txtDoor12_05.Text = m_ALDoors[04].ToString();
                        Door12Sub000001_01.txtDoor12_06.Text = m_ALDoors[05].ToString();
                        break;
                    case 05:
                        Door12Sub000001_01.ckbDoor12_01.Checked = true;
                        Door12Sub000001_01.ckbDoor12_02.Checked = true;
                        Door12Sub000001_01.ckbDoor12_03.Checked = true;
                        Door12Sub000001_01.ckbDoor12_04.Checked = true;
                        Door12Sub000001_01.ckbDoor12_05.Checked = true;
                        Door12Sub000001_01.txtDoor12_01.Text = m_ALDoors[00].ToString();
                        Door12Sub000001_01.txtDoor12_02.Text = m_ALDoors[01].ToString();
                        Door12Sub000001_01.txtDoor12_03.Text = m_ALDoors[02].ToString();
                        Door12Sub000001_01.txtDoor12_04.Text = m_ALDoors[03].ToString();
                        Door12Sub000001_01.txtDoor12_05.Text = m_ALDoors[04].ToString();
                        break;
                    case 04:
                        Door12Sub000001_01.ckbDoor12_01.Checked = true;
                        Door12Sub000001_01.ckbDoor12_02.Checked = true;
                        Door12Sub000001_01.ckbDoor12_03.Checked = true;
                        Door12Sub000001_01.ckbDoor12_04.Checked = true;
                        Door12Sub000001_01.txtDoor12_01.Text = m_ALDoors[00].ToString();
                        Door12Sub000001_01.txtDoor12_02.Text = m_ALDoors[01].ToString();
                        Door12Sub000001_01.txtDoor12_03.Text = m_ALDoors[02].ToString();
                        Door12Sub000001_01.txtDoor12_04.Text = m_ALDoors[03].ToString();
                        break;
                    case 03:
                        Door12Sub000001_01.ckbDoor12_01.Checked = true;
                        Door12Sub000001_01.ckbDoor12_02.Checked = true;
                        Door12Sub000001_01.ckbDoor12_03.Checked = true;
                        Door12Sub000001_01.txtDoor12_01.Text = m_ALDoors[00].ToString();
                        Door12Sub000001_01.txtDoor12_02.Text = m_ALDoors[01].ToString();
                        Door12Sub000001_01.txtDoor12_03.Text = m_ALDoors[02].ToString();
                        break;
                    case 02:
                        Door12Sub000001_01.ckbDoor12_01.Checked = true;
                        Door12Sub000001_01.ckbDoor12_02.Checked = true;
                        Door12Sub000001_01.txtDoor12_01.Text = m_ALDoors[00].ToString();
                        Door12Sub000001_01.txtDoor12_02.Text = m_ALDoors[01].ToString();
                        break;
                    case 01:
                        Door12Sub000001_01.ckbDoor12_01.Checked = true;
                        Door12Sub000001_01.txtDoor12_01.Text = m_ALDoors[00].ToString();
                        break;
                }
            }
            else// A/B
            {
                if (m_ALDoors.Count > 4)
                {
                    AB12LEVEL4Sub000001_01.textBox1.Text = m_ALDoors[00].ToString();
                    AB12LEVEL4Sub000001_01.textBox2.Text = m_ALDoors[01].ToString();
                    AB12LEVEL4Sub000001_01.textBox3.Text = m_ALDoors[02].ToString();
                    AB12LEVEL4Sub000001_01.textBox4.Text = m_ALDoors[03].ToString();
                    AB12LEVEL4Sub000001_01.textBox5.Text = m_ALDoors[04].ToString();
                    AB12LEVEL4Sub000001_01.textBox6.Text = m_ALDoors[05].ToString();
                    AB12LEVEL4Sub000001_01.textBox7.Text = m_ALDoors[06].ToString();
                    AB12LEVEL4Sub000001_01.textBox8.Text = m_ALDoors[07].ToString();
                    AB12LEVEL4Sub000001_01.textBox9.Text = m_ALDoors[08].ToString();
                    AB12LEVEL4Sub000001_01.textBox10.Text = m_ALDoors[09].ToString();
                    AB12LEVEL4Sub000001_01.textBox11.Text = m_ALDoors[10].ToString();
                    AB12LEVEL4Sub000001_01.textBox12.Text = m_ALDoors[11].ToString();

                    AB12LEVEL3Sub000001_01.textBox1.Text = m_ALDoors[00].ToString();
                    AB12LEVEL3Sub000001_01.textBox2.Text = m_ALDoors[01].ToString();
                    AB12LEVEL3Sub000001_01.textBox3.Text = m_ALDoors[02].ToString();
                    AB12LEVEL3Sub000001_01.textBox4.Text = m_ALDoors[03].ToString();
                    AB12LEVEL3Sub000001_01.textBox5.Text = m_ALDoors[04].ToString();
                    AB12LEVEL3Sub000001_01.textBox6.Text = m_ALDoors[05].ToString();
                    AB12LEVEL3Sub000001_01.textBox7.Text = m_ALDoors[06].ToString();
                    AB12LEVEL3Sub000001_01.textBox8.Text = m_ALDoors[07].ToString();
                    AB12LEVEL3Sub000001_01.textBox9.Text = m_ALDoors[08].ToString();
                    AB12LEVEL3Sub000001_01.textBox10.Text = m_ALDoors[09].ToString();
                    AB12LEVEL3Sub000001_01.textBox11.Text = m_ALDoors[10].ToString();
                    AB12LEVEL3Sub000001_01.textBox12.Text = m_ALDoors[11].ToString();

                    AB12LEVEL2Sub000001_01.textBox1.Text = m_ALDoors[00].ToString();
                    AB12LEVEL2Sub000001_01.textBox2.Text = m_ALDoors[01].ToString();
                    AB12LEVEL2Sub000001_01.textBox3.Text = m_ALDoors[02].ToString();
                    AB12LEVEL2Sub000001_01.textBox4.Text = m_ALDoors[03].ToString();
                    AB12LEVEL2Sub000001_01.textBox5.Text = m_ALDoors[04].ToString();
                    AB12LEVEL2Sub000001_01.textBox6.Text = m_ALDoors[05].ToString();
                    AB12LEVEL2Sub000001_01.textBox7.Text = m_ALDoors[06].ToString();
                    AB12LEVEL2Sub000001_01.textBox8.Text = m_ALDoors[07].ToString();
                    AB12LEVEL2Sub000001_01.textBox9.Text = m_ALDoors[08].ToString();
                    AB12LEVEL2Sub000001_01.textBox10.Text = m_ALDoors[09].ToString();
                    AB12LEVEL2Sub000001_01.textBox11.Text = m_ALDoors[10].ToString();
                    AB12LEVEL2Sub000001_01.textBox12.Text = m_ALDoors[11].ToString();

                    AB12LEVEL1Sub000001_01.textBox1.Text = m_ALDoors[00].ToString();
                    AB12LEVEL1Sub000001_01.textBox2.Text = m_ALDoors[01].ToString();
                    AB12LEVEL1Sub000001_01.textBox3.Text = m_ALDoors[02].ToString();
                    AB12LEVEL1Sub000001_01.textBox4.Text = m_ALDoors[03].ToString();
                    AB12LEVEL1Sub000001_01.textBox5.Text = m_ALDoors[04].ToString();
                    AB12LEVEL1Sub000001_01.textBox6.Text = m_ALDoors[05].ToString();
                    AB12LEVEL1Sub000001_01.textBox7.Text = m_ALDoors[06].ToString();
                    AB12LEVEL1Sub000001_01.textBox8.Text = m_ALDoors[07].ToString();
                    AB12LEVEL1Sub000001_01.textBox9.Text = m_ALDoors[08].ToString();
                    AB12LEVEL1Sub000001_01.textBox10.Text = m_ALDoors[09].ToString();
                    AB12LEVEL1Sub000001_01.textBox11.Text = m_ALDoors[10].ToString();
                    AB12LEVEL1Sub000001_01.textBox12.Text = m_ALDoors[11].ToString();
                }
                else
                {
                    AB4LEVEL4Sub000001_01.textBox1.Text = m_ALDoors[00].ToString();
                    AB4LEVEL4Sub000001_01.textBox2.Text = m_ALDoors[01].ToString();
                    AB4LEVEL4Sub000001_01.textBox3.Text = m_ALDoors[02].ToString();
                    AB4LEVEL4Sub000001_01.textBox4.Text = m_ALDoors[03].ToString();

                    AB4LEVEL3Sub000001_01.textBox1.Text = m_ALDoors[00].ToString();
                    AB4LEVEL3Sub000001_01.textBox2.Text = m_ALDoors[01].ToString();
                    AB4LEVEL3Sub000001_01.textBox3.Text = m_ALDoors[02].ToString();
                    AB4LEVEL3Sub000001_01.textBox4.Text = m_ALDoors[03].ToString();

                    AB4LEVEL2Sub000001_01.textBox1.Text = m_ALDoors[00].ToString();
                    AB4LEVEL2Sub000001_01.textBox2.Text = m_ALDoors[01].ToString();
                    AB4LEVEL2Sub000001_01.textBox3.Text = m_ALDoors[02].ToString();
                    AB4LEVEL2Sub000001_01.textBox4.Text = m_ALDoors[03].ToString();

                    AB4LEVEL1Sub000001_01.textBox1.Text = m_ALDoors[00].ToString();
                    AB4LEVEL1Sub000001_01.textBox2.Text = m_ALDoors[01].ToString();
                    AB4LEVEL1Sub000001_01.textBox3.Text = m_ALDoors[02].ToString();
                    AB4LEVEL1Sub000001_01.textBox4.Text = m_ALDoors[03].ToString();
                }
            }
            //--
            m_tabSub000001.Parent = m_tabMain;
            m_tabMain.SelectedTab = m_tabSub000001;

            //--
            //隱藏目前沒有功能的按鈕 at 2017/06/26
            butSub000001_08.Visible = false;
            butSub000001_09.Visible = false;
            butSub000001_10.Visible = false;
            butSub000001_11.Visible = false;
            //--
            //--
            //禁止再次編修項目
            cmbSub000001_01.Enabled = false;
            cmbSub000001_03.Enabled = false;
            labSub000001_09.Enabled = false;
            txtSub000001_04.Enabled = false;// add at 2017/08/07
            txtSub000001_03.Enabled = false;// add at 2017/08/07
            //--
            //--
            //butSub000001_12.Visible = true;
            //butSub000001_13.Visible = false;
            //--

            //--
            //add at 2017/10/05
            m_Sub000001ALInit.Clear();

            m_Sub000001ALInit.Add(txtSub000001_01.Text);//add at 2017/10/05
            m_Sub000001ALInit.Add(txtSub000001_02.Text);//add at 2017/10/05
            m_Sub000001ALInit.Add(labSub000001_09.Text);//add at 2017/10/05
            m_Sub000001ALInit.Add(txtSub000001_03.Text);//add at 2017/10/05
            m_Sub000001ALInit.Add(txtSub000001_04.Text);//add at 2017/10/05
            m_Sub000001ALInit.Add(rdbSub000001_01.Checked.ToString());//add at 2017/10/05
            m_Sub000001ALInit.Add(ckbSub000001_01.Checked.ToString());//add at 2017/10/05
            m_Sub000001ALInit.Add(rdbSub000001_03.Checked.ToString());//add at 2017/10/05
            m_Sub000001ALInit.Add(rdbSub000001_04.Checked.ToString());//add at 2017/10/05
            m_Sub000001ALInit.Add(ckbSub000001_02.Checked.ToString());//add at 2017/10/05
            m_Sub000001ALInit.Add(txtSub000001_19.Value + "");//add at 2017/10/05
            m_Sub000001ALInit.Add(cmbSub000001_01.SelectedIndex + "");//add at 2017/10/05
            m_Sub000001ALInit.Add(cmbSub000001_03.SelectedIndex + "");//add at 2017/10/05
            m_Sub000001ALInit.Add(cmbSub000001_04.SelectedIndex + "");//SYDM 選擇元件 修改偵測

            for (int i = 0; i < m_ALSub000001Date.Count; i++)//add at 2017/10/05
            {
                m_Sub000001ALInit.Add(m_ALSub000001Date[i].ToString());
            }

            for (int j = 0; j < m_ALDoors.Count; j++)//add at 2017/10/05
            {
                m_Sub000001ALInit.Add(m_ALDoors[j].ToString());
            }
            //--
            //--
            //add 2017/10/19
            labSub000001_03.ForeColor = Color.Black;
            labSub000001_01.ForeColor = Color.Black;
            labSub000001_04.ForeColor = Color.Black;
            labSub000001_20.ForeColor = Color.Black;//SYCG模式下新增控制器一定要有SYDM的防呆機制
            //--
            
            */

            setSub000001UI();
            //控制器UI多選編輯實作 ~ 把顯示控制器參數獨立成函數[setSub000001UI]
        }

        //---
        //控制器UI多選編輯實作 ~ 把顯示控制器參數獨立成函數[setSub000001UI]
        public void setSub000001UI()
        {
            String SQL = "";

            m_intdgvSub0000_01_SN = -1;
            initSub000001UI(true, "" + m_intcontroller_sn);//要把假日列表，依控制器獨立切開 at 2017/08/16 -- initSub000001UI();//用來切換子元件顯示-2017/03/03
            cmbSub000001_01.Enabled = false;//控制器在SERVER mode 時 SN 自動填入 日+時+分+秒
            //--
            //取出對應的controller 表的資料
            String Strsn, Strmodel, Strname, Stralias, Strdoor_number;
            SQL = String.Format("SELECT c.sydm_id AS sydm_id,c.sn AS sn,c.model AS model,c.name AS name,c.alias AS alias,c_e.door_number AS door_number FROM controller AS c,controller_extend AS c_e WHERE c.sn=c_e.controller_sn AND c.sn={0};", m_intcontroller_sn);//為了新增可以區分4/12門控制器 所以要修改 at 2017/07/28
            MySqlDataReader controllerReader = MySQL.GetDataReader(SQL);
            while (controllerReader.Read())
            {
                m_intdgvSub0000_01_SN = m_intcontroller_sn;
                Strsn = controllerReader["sn"].ToString();
                Strmodel = controllerReader["model"].ToString();
                Strname = controllerReader["name"].ToString();
                Stralias = controllerReader["alias"].ToString();
                Strdoor_number = controllerReader["door_number"].ToString();//為了新增可以區分4/12門控制器 所以要修改 at 2017/07/28
                txtSub000001_01.Text = Strname;
                txtSub000001_02.Text = Stralias;
                for (int i = 0; i < HW_Net_API.m_ALHW_ID.Count; i++)
                {
                    if ((Convert.ToInt32(HW_Net_API.m_ALHW_ID[i].ToString(), 16) == Convert.ToInt32(Strmodel)) && (Strdoor_number == HW_Net_API.m_ALHW_DoorNumber[i].ToString()))//為了新增可以區分4/12門控制器 所以要修改 at 2017/07/28
                    {
                        cmbSub000001_03.SelectedIndex = i;
                        break;
                    }
                }
                labSub000001_09.Text = Strsn;

                //---
                //SYDM 選擇元件在指定控制器時顯示對應值
                cmbSub000001_04.Enabled = false;
                cmbSub000001_04.SelectedIndex = -1;//防止SYDM被刪
                if (controllerReader["sydm_id"].ToString() == "0")
                {
                    cmbSub000001_04.SelectedIndex = -1;
                }
                else
                {
                    for (int i = 0; i < m_ALSYDM_ID.Count; i++)
                    {
                        if (((String)m_ALSYDM_ID[i]) == controllerReader["sydm_id"].ToString())
                        {
                            cmbSub000001_04.SelectedIndex = i;
                        }
                    }
                }
                //---

                m_intSYDM_id = Convert.ToInt32(controllerReader["sydm_id"].ToString());//SYCG模式下-建立/暫存 當下要操作的SYDM ID

                break;
            }
            controllerReader.Close();
            if (m_intdgvSub0000_01_SN < 0)
            {
                return;//沒有選擇修改項目，防呆
            }
            //--
            //--
            //取出對應的controller_extend 表的資料
            String Strconnetction_address, Strconnetction_enabled, Strconnetction_mode, Strapb_enable, Strapb_mode, Strapb_group, Strapb_level_list, Strapb_reset_timestamp_list, Strab_door_enabled, Strab_door_level, Strab_door_timeout_second, Strab_door_reset_time_second, Strsame_card_interval_time_second;
            Strab_door_enabled = "";
            SQL = String.Format("SELECT port,connetction_address,connetction_enabled,connetction_mode,apb_enable,apb_mode,apb_group,apb_level_list,apb_reset_timestamp_list,ab_door_enabled,ab_door_level,ab_door_timeout_second,ab_door_reset_time_second,same_card_interval_time_second FROM controller_extend WHERE controller_sn={0};", m_intcontroller_sn);
            MySqlDataReader controller_extendReader = MySQL.GetDataReader(SQL);
            while (controller_extendReader.Read())
            {
                Strsame_card_interval_time_second = controller_extendReader["same_card_interval_time_second"].ToString();//add 2017/0822
                Strconnetction_address = controller_extendReader["connetction_address"].ToString();
                Strconnetction_enabled = controller_extendReader["connetction_enabled"].ToString();
                Strconnetction_mode = controller_extendReader["connetction_mode"].ToString();
                Strapb_enable = controller_extendReader["apb_enable"].ToString();
                Strapb_mode = controller_extendReader["apb_mode"].ToString();
                Strapb_group = controller_extendReader["apb_group"].ToString();
                Strapb_level_list = controller_extendReader["apb_level_list"].ToString();
                Strapb_reset_timestamp_list = controller_extendReader["apb_reset_timestamp_list"].ToString();
                Strab_door_enabled = controller_extendReader["ab_door_enabled"].ToString();
                Strab_door_level = controller_extendReader["ab_door_level"].ToString();
                Strab_door_timeout_second = controller_extendReader["ab_door_timeout_second"].ToString();
                Strab_door_reset_time_second = controller_extendReader["ab_door_reset_time_second"].ToString();
                txtSub000001_03.Text = controller_extendReader["port"].ToString();//按照『V8 功能選單』一個一個改 - 控制器列表 ~ 修正控制器顯示通訊PORT的BUG
                txtSub000001_04.Text = Strconnetction_address;
                txtSub000001_07.Text = Strsame_card_interval_time_second;//add 2017/0822
                if (Strconnetction_enabled != "0")
                {
                    rdbSub000001_01.Checked = true;
                }
                else
                {
                    rdbSub000001_02.Checked = true;
                }
                cmbSub000001_01.SelectedIndex = Convert.ToInt32(Strconnetction_mode);
                if (Strapb_enable != "0")
                {
                    ckbSub000001_01.Checked = true;
                }
                else
                {
                    ckbSub000001_01.Checked = false;
                }

                rdbSub000001_03.Checked = false;
                rdbSub000001_04.Checked = false;
                if (Strapb_mode == "1")
                {
                    rdbSub000001_03.Checked = true;
                }
                else if (Strapb_mode == "2")
                {
                    rdbSub000001_04.Checked = true;
                }

                //Strapb_group
                //Strapb_level_list
                //Strapb_reset_timestamp_list
                txtSub000001_19.Value = Convert.ToInt32(Strab_door_level);
                if (Strab_door_enabled != "0")
                {
                    ckbSub000001_02.Checked = true;

                    AB12LEVEL4Sub000001_01.jlNumEdit1.Value = Convert.ToInt32(Strab_door_timeout_second);
                    AB12LEVEL4Sub000001_01.jlNumEdit2.Value = Convert.ToInt32(Strab_door_reset_time_second);

                    AB12LEVEL3Sub000001_01.jlNumEdit1.Value = Convert.ToInt32(Strab_door_timeout_second);
                    AB12LEVEL3Sub000001_01.jlNumEdit2.Value = Convert.ToInt32(Strab_door_reset_time_second);

                    AB12LEVEL2Sub000001_01.jlNumEdit1.Value = Convert.ToInt32(Strab_door_timeout_second);
                    AB12LEVEL2Sub000001_01.jlNumEdit2.Value = Convert.ToInt32(Strab_door_reset_time_second);

                    AB4LEVEL4Sub000001_01.jlNumEdit1.Value = Convert.ToInt32(Strab_door_timeout_second);
                    AB4LEVEL4Sub000001_01.jlNumEdit2.Value = Convert.ToInt32(Strab_door_reset_time_second);

                    AB4LEVEL3Sub000001_01.jlNumEdit1.Value = Convert.ToInt32(Strab_door_timeout_second);
                    AB4LEVEL3Sub000001_01.jlNumEdit2.Value = Convert.ToInt32(Strab_door_reset_time_second);

                    AB4LEVEL2Sub000001_01.jlNumEdit1.Value = Convert.ToInt32(Strab_door_timeout_second);
                    AB4LEVEL2Sub000001_01.jlNumEdit2.Value = Convert.ToInt32(Strab_door_reset_time_second);
                }
                else
                {
                    ckbSub000001_02.Checked = false;
                }
                ChangeSub000001UI(true);//觸發置換下方子元件

                break;
            }
            controller_extendReader.Close();
            //--
            //--
            //取出對應的door 表的資料
            m_ALDoors.Clear();
            m_ALDoor_id.Clear();
            SQL = String.Format("SELECT id,name FROM door WHERE controller_id={0} ORDER BY id ASC;", m_intcontroller_sn);
            MySqlDataReader doorReader = MySQL.GetDataReader(SQL);
            while (doorReader.Read())
            {
                m_ALDoor_id.Add(doorReader["id"].ToString());
                m_ALDoors.Add(doorReader["name"].ToString());
            }
            doorReader.Close();
            if (Strab_door_enabled == "0")//一般
            {
                switch (m_ALDoors.Count)
                {
                    //--
                    //add 2017/10/19
                    case 128:
                        egsSub000001_01.m_ALAllName.Clear();
                        for (int i = 0; i < m_ALDoors.Count; i++)
                        {
                            egsSub000001_01.m_ALAllName.Add(m_ALDoors[i].ToString());
                        }
                        egsSub000001_01.SetAllName();
                        break;
                    //--
                    case 12:
                        Door12Sub000001_01.ckbDoor12_01.Checked = true;
                        Door12Sub000001_01.ckbDoor12_02.Checked = true;
                        Door12Sub000001_01.ckbDoor12_03.Checked = true;
                        Door12Sub000001_01.ckbDoor12_04.Checked = true;
                        Door12Sub000001_01.ckbDoor12_05.Checked = true;
                        Door12Sub000001_01.ckbDoor12_06.Checked = true;
                        Door12Sub000001_01.ckbDoor12_07.Checked = true;
                        Door12Sub000001_01.ckbDoor12_08.Checked = true;
                        Door12Sub000001_01.ckbDoor12_09.Checked = true;
                        Door12Sub000001_01.ckbDoor12_10.Checked = true;
                        Door12Sub000001_01.ckbDoor12_11.Checked = true;
                        Door12Sub000001_01.ckbDoor12_12.Checked = true;
                        Door12Sub000001_01.txtDoor12_01.Text = m_ALDoors[00].ToString();
                        Door12Sub000001_01.txtDoor12_02.Text = m_ALDoors[01].ToString();
                        Door12Sub000001_01.txtDoor12_03.Text = m_ALDoors[02].ToString();
                        Door12Sub000001_01.txtDoor12_04.Text = m_ALDoors[03].ToString();
                        Door12Sub000001_01.txtDoor12_05.Text = m_ALDoors[04].ToString();
                        Door12Sub000001_01.txtDoor12_06.Text = m_ALDoors[05].ToString();
                        Door12Sub000001_01.txtDoor12_07.Text = m_ALDoors[06].ToString();
                        Door12Sub000001_01.txtDoor12_08.Text = m_ALDoors[07].ToString();
                        Door12Sub000001_01.txtDoor12_09.Text = m_ALDoors[08].ToString();
                        Door12Sub000001_01.txtDoor12_10.Text = m_ALDoors[09].ToString();
                        Door12Sub000001_01.txtDoor12_11.Text = m_ALDoors[10].ToString();
                        Door12Sub000001_01.txtDoor12_12.Text = m_ALDoors[11].ToString();
                        break;
                    case 11:
                        Door12Sub000001_01.ckbDoor12_01.Checked = true;
                        Door12Sub000001_01.ckbDoor12_02.Checked = true;
                        Door12Sub000001_01.ckbDoor12_03.Checked = true;
                        Door12Sub000001_01.ckbDoor12_04.Checked = true;
                        Door12Sub000001_01.ckbDoor12_05.Checked = true;
                        Door12Sub000001_01.ckbDoor12_06.Checked = true;
                        Door12Sub000001_01.ckbDoor12_07.Checked = true;
                        Door12Sub000001_01.ckbDoor12_08.Checked = true;
                        Door12Sub000001_01.ckbDoor12_09.Checked = true;
                        Door12Sub000001_01.ckbDoor12_10.Checked = true;
                        Door12Sub000001_01.ckbDoor12_11.Checked = true;
                        Door12Sub000001_01.txtDoor12_01.Text = m_ALDoors[00].ToString();
                        Door12Sub000001_01.txtDoor12_02.Text = m_ALDoors[01].ToString();
                        Door12Sub000001_01.txtDoor12_03.Text = m_ALDoors[02].ToString();
                        Door12Sub000001_01.txtDoor12_04.Text = m_ALDoors[03].ToString();
                        Door12Sub000001_01.txtDoor12_05.Text = m_ALDoors[04].ToString();
                        Door12Sub000001_01.txtDoor12_06.Text = m_ALDoors[05].ToString();
                        Door12Sub000001_01.txtDoor12_07.Text = m_ALDoors[06].ToString();
                        Door12Sub000001_01.txtDoor12_08.Text = m_ALDoors[07].ToString();
                        Door12Sub000001_01.txtDoor12_09.Text = m_ALDoors[08].ToString();
                        Door12Sub000001_01.txtDoor12_10.Text = m_ALDoors[09].ToString();
                        Door12Sub000001_01.txtDoor12_11.Text = m_ALDoors[10].ToString();
                        break;
                    case 10:
                        Door12Sub000001_01.ckbDoor12_01.Checked = true;
                        Door12Sub000001_01.ckbDoor12_02.Checked = true;
                        Door12Sub000001_01.ckbDoor12_03.Checked = true;
                        Door12Sub000001_01.ckbDoor12_04.Checked = true;
                        Door12Sub000001_01.ckbDoor12_05.Checked = true;
                        Door12Sub000001_01.ckbDoor12_06.Checked = true;
                        Door12Sub000001_01.ckbDoor12_07.Checked = true;
                        Door12Sub000001_01.ckbDoor12_08.Checked = true;
                        Door12Sub000001_01.ckbDoor12_09.Checked = true;
                        Door12Sub000001_01.ckbDoor12_10.Checked = true;
                        Door12Sub000001_01.txtDoor12_01.Text = m_ALDoors[00].ToString();
                        Door12Sub000001_01.txtDoor12_02.Text = m_ALDoors[01].ToString();
                        Door12Sub000001_01.txtDoor12_03.Text = m_ALDoors[02].ToString();
                        Door12Sub000001_01.txtDoor12_04.Text = m_ALDoors[03].ToString();
                        Door12Sub000001_01.txtDoor12_05.Text = m_ALDoors[04].ToString();
                        Door12Sub000001_01.txtDoor12_06.Text = m_ALDoors[05].ToString();
                        Door12Sub000001_01.txtDoor12_07.Text = m_ALDoors[06].ToString();
                        Door12Sub000001_01.txtDoor12_08.Text = m_ALDoors[07].ToString();
                        Door12Sub000001_01.txtDoor12_09.Text = m_ALDoors[08].ToString();
                        Door12Sub000001_01.txtDoor12_10.Text = m_ALDoors[09].ToString();
                        break;
                    case 09:
                        Door12Sub000001_01.ckbDoor12_01.Checked = true;
                        Door12Sub000001_01.ckbDoor12_02.Checked = true;
                        Door12Sub000001_01.ckbDoor12_03.Checked = true;
                        Door12Sub000001_01.ckbDoor12_04.Checked = true;
                        Door12Sub000001_01.ckbDoor12_05.Checked = true;
                        Door12Sub000001_01.ckbDoor12_06.Checked = true;
                        Door12Sub000001_01.ckbDoor12_07.Checked = true;
                        Door12Sub000001_01.ckbDoor12_08.Checked = true;
                        Door12Sub000001_01.ckbDoor12_09.Checked = true;
                        Door12Sub000001_01.txtDoor12_01.Text = m_ALDoors[00].ToString();
                        Door12Sub000001_01.txtDoor12_02.Text = m_ALDoors[01].ToString();
                        Door12Sub000001_01.txtDoor12_03.Text = m_ALDoors[02].ToString();
                        Door12Sub000001_01.txtDoor12_04.Text = m_ALDoors[03].ToString();
                        Door12Sub000001_01.txtDoor12_05.Text = m_ALDoors[04].ToString();
                        Door12Sub000001_01.txtDoor12_06.Text = m_ALDoors[05].ToString();
                        Door12Sub000001_01.txtDoor12_07.Text = m_ALDoors[06].ToString();
                        Door12Sub000001_01.txtDoor12_08.Text = m_ALDoors[07].ToString();
                        Door12Sub000001_01.txtDoor12_09.Text = m_ALDoors[08].ToString();
                        break;
                    case 08:
                        Door12Sub000001_01.ckbDoor12_01.Checked = true;
                        Door12Sub000001_01.ckbDoor12_02.Checked = true;
                        Door12Sub000001_01.ckbDoor12_03.Checked = true;
                        Door12Sub000001_01.ckbDoor12_04.Checked = true;
                        Door12Sub000001_01.ckbDoor12_05.Checked = true;
                        Door12Sub000001_01.ckbDoor12_06.Checked = true;
                        Door12Sub000001_01.ckbDoor12_07.Checked = true;
                        Door12Sub000001_01.ckbDoor12_08.Checked = true;
                        Door12Sub000001_01.txtDoor12_01.Text = m_ALDoors[00].ToString();
                        Door12Sub000001_01.txtDoor12_02.Text = m_ALDoors[01].ToString();
                        Door12Sub000001_01.txtDoor12_03.Text = m_ALDoors[02].ToString();
                        Door12Sub000001_01.txtDoor12_04.Text = m_ALDoors[03].ToString();
                        Door12Sub000001_01.txtDoor12_05.Text = m_ALDoors[04].ToString();
                        Door12Sub000001_01.txtDoor12_06.Text = m_ALDoors[05].ToString();
                        Door12Sub000001_01.txtDoor12_07.Text = m_ALDoors[06].ToString();
                        Door12Sub000001_01.txtDoor12_08.Text = m_ALDoors[07].ToString();
                        break;
                    case 07:
                        Door12Sub000001_01.ckbDoor12_01.Checked = true;
                        Door12Sub000001_01.ckbDoor12_02.Checked = true;
                        Door12Sub000001_01.ckbDoor12_03.Checked = true;
                        Door12Sub000001_01.ckbDoor12_04.Checked = true;
                        Door12Sub000001_01.ckbDoor12_05.Checked = true;
                        Door12Sub000001_01.ckbDoor12_06.Checked = true;
                        Door12Sub000001_01.ckbDoor12_07.Checked = true;
                        Door12Sub000001_01.txtDoor12_01.Text = m_ALDoors[00].ToString();
                        Door12Sub000001_01.txtDoor12_02.Text = m_ALDoors[01].ToString();
                        Door12Sub000001_01.txtDoor12_03.Text = m_ALDoors[02].ToString();
                        Door12Sub000001_01.txtDoor12_04.Text = m_ALDoors[03].ToString();
                        Door12Sub000001_01.txtDoor12_05.Text = m_ALDoors[04].ToString();
                        Door12Sub000001_01.txtDoor12_06.Text = m_ALDoors[05].ToString();
                        Door12Sub000001_01.txtDoor12_07.Text = m_ALDoors[06].ToString();
                        break;
                    case 06:
                        Door12Sub000001_01.ckbDoor12_01.Checked = true;
                        Door12Sub000001_01.ckbDoor12_02.Checked = true;
                        Door12Sub000001_01.ckbDoor12_03.Checked = true;
                        Door12Sub000001_01.ckbDoor12_04.Checked = true;
                        Door12Sub000001_01.ckbDoor12_05.Checked = true;
                        Door12Sub000001_01.ckbDoor12_06.Checked = true;
                        Door12Sub000001_01.txtDoor12_01.Text = m_ALDoors[00].ToString();
                        Door12Sub000001_01.txtDoor12_02.Text = m_ALDoors[01].ToString();
                        Door12Sub000001_01.txtDoor12_03.Text = m_ALDoors[02].ToString();
                        Door12Sub000001_01.txtDoor12_04.Text = m_ALDoors[03].ToString();
                        Door12Sub000001_01.txtDoor12_05.Text = m_ALDoors[04].ToString();
                        Door12Sub000001_01.txtDoor12_06.Text = m_ALDoors[05].ToString();
                        break;
                    case 05:
                        Door12Sub000001_01.ckbDoor12_01.Checked = true;
                        Door12Sub000001_01.ckbDoor12_02.Checked = true;
                        Door12Sub000001_01.ckbDoor12_03.Checked = true;
                        Door12Sub000001_01.ckbDoor12_04.Checked = true;
                        Door12Sub000001_01.ckbDoor12_05.Checked = true;
                        Door12Sub000001_01.txtDoor12_01.Text = m_ALDoors[00].ToString();
                        Door12Sub000001_01.txtDoor12_02.Text = m_ALDoors[01].ToString();
                        Door12Sub000001_01.txtDoor12_03.Text = m_ALDoors[02].ToString();
                        Door12Sub000001_01.txtDoor12_04.Text = m_ALDoors[03].ToString();
                        Door12Sub000001_01.txtDoor12_05.Text = m_ALDoors[04].ToString();
                        break;
                    case 04:
                        Door12Sub000001_01.ckbDoor12_01.Checked = true;
                        Door12Sub000001_01.ckbDoor12_02.Checked = true;
                        Door12Sub000001_01.ckbDoor12_03.Checked = true;
                        Door12Sub000001_01.ckbDoor12_04.Checked = true;
                        Door12Sub000001_01.txtDoor12_01.Text = m_ALDoors[00].ToString();
                        Door12Sub000001_01.txtDoor12_02.Text = m_ALDoors[01].ToString();
                        Door12Sub000001_01.txtDoor12_03.Text = m_ALDoors[02].ToString();
                        Door12Sub000001_01.txtDoor12_04.Text = m_ALDoors[03].ToString();
                        break;
                    case 03:
                        Door12Sub000001_01.ckbDoor12_01.Checked = true;
                        Door12Sub000001_01.ckbDoor12_02.Checked = true;
                        Door12Sub000001_01.ckbDoor12_03.Checked = true;
                        Door12Sub000001_01.txtDoor12_01.Text = m_ALDoors[00].ToString();
                        Door12Sub000001_01.txtDoor12_02.Text = m_ALDoors[01].ToString();
                        Door12Sub000001_01.txtDoor12_03.Text = m_ALDoors[02].ToString();
                        break;
                    case 02:
                        Door12Sub000001_01.ckbDoor12_01.Checked = true;
                        Door12Sub000001_01.ckbDoor12_02.Checked = true;
                        Door12Sub000001_01.txtDoor12_01.Text = m_ALDoors[00].ToString();
                        Door12Sub000001_01.txtDoor12_02.Text = m_ALDoors[01].ToString();
                        break;
                    case 01:
                        Door12Sub000001_01.ckbDoor12_01.Checked = true;
                        Door12Sub000001_01.txtDoor12_01.Text = m_ALDoors[00].ToString();
                        break;
                }
            }
            else// A/B
            {
                if (m_ALDoors.Count > 4)
                {
                    AB12LEVEL4Sub000001_01.textBox1.Text = m_ALDoors[00].ToString();
                    AB12LEVEL4Sub000001_01.textBox2.Text = m_ALDoors[01].ToString();
                    AB12LEVEL4Sub000001_01.textBox3.Text = m_ALDoors[02].ToString();
                    AB12LEVEL4Sub000001_01.textBox4.Text = m_ALDoors[03].ToString();
                    AB12LEVEL4Sub000001_01.textBox5.Text = m_ALDoors[04].ToString();
                    AB12LEVEL4Sub000001_01.textBox6.Text = m_ALDoors[05].ToString();
                    AB12LEVEL4Sub000001_01.textBox7.Text = m_ALDoors[06].ToString();
                    AB12LEVEL4Sub000001_01.textBox8.Text = m_ALDoors[07].ToString();
                    AB12LEVEL4Sub000001_01.textBox9.Text = m_ALDoors[08].ToString();
                    AB12LEVEL4Sub000001_01.textBox10.Text = m_ALDoors[09].ToString();
                    AB12LEVEL4Sub000001_01.textBox11.Text = m_ALDoors[10].ToString();
                    AB12LEVEL4Sub000001_01.textBox12.Text = m_ALDoors[11].ToString();

                    AB12LEVEL3Sub000001_01.textBox1.Text = m_ALDoors[00].ToString();
                    AB12LEVEL3Sub000001_01.textBox2.Text = m_ALDoors[01].ToString();
                    AB12LEVEL3Sub000001_01.textBox3.Text = m_ALDoors[02].ToString();
                    AB12LEVEL3Sub000001_01.textBox4.Text = m_ALDoors[03].ToString();
                    AB12LEVEL3Sub000001_01.textBox5.Text = m_ALDoors[04].ToString();
                    AB12LEVEL3Sub000001_01.textBox6.Text = m_ALDoors[05].ToString();
                    AB12LEVEL3Sub000001_01.textBox7.Text = m_ALDoors[06].ToString();
                    AB12LEVEL3Sub000001_01.textBox8.Text = m_ALDoors[07].ToString();
                    AB12LEVEL3Sub000001_01.textBox9.Text = m_ALDoors[08].ToString();
                    AB12LEVEL3Sub000001_01.textBox10.Text = m_ALDoors[09].ToString();
                    AB12LEVEL3Sub000001_01.textBox11.Text = m_ALDoors[10].ToString();
                    AB12LEVEL3Sub000001_01.textBox12.Text = m_ALDoors[11].ToString();

                    AB12LEVEL2Sub000001_01.textBox1.Text = m_ALDoors[00].ToString();
                    AB12LEVEL2Sub000001_01.textBox2.Text = m_ALDoors[01].ToString();
                    AB12LEVEL2Sub000001_01.textBox3.Text = m_ALDoors[02].ToString();
                    AB12LEVEL2Sub000001_01.textBox4.Text = m_ALDoors[03].ToString();
                    AB12LEVEL2Sub000001_01.textBox5.Text = m_ALDoors[04].ToString();
                    AB12LEVEL2Sub000001_01.textBox6.Text = m_ALDoors[05].ToString();
                    AB12LEVEL2Sub000001_01.textBox7.Text = m_ALDoors[06].ToString();
                    AB12LEVEL2Sub000001_01.textBox8.Text = m_ALDoors[07].ToString();
                    AB12LEVEL2Sub000001_01.textBox9.Text = m_ALDoors[08].ToString();
                    AB12LEVEL2Sub000001_01.textBox10.Text = m_ALDoors[09].ToString();
                    AB12LEVEL2Sub000001_01.textBox11.Text = m_ALDoors[10].ToString();
                    AB12LEVEL2Sub000001_01.textBox12.Text = m_ALDoors[11].ToString();

                    AB12LEVEL1Sub000001_01.textBox1.Text = m_ALDoors[00].ToString();
                    AB12LEVEL1Sub000001_01.textBox2.Text = m_ALDoors[01].ToString();
                    AB12LEVEL1Sub000001_01.textBox3.Text = m_ALDoors[02].ToString();
                    AB12LEVEL1Sub000001_01.textBox4.Text = m_ALDoors[03].ToString();
                    AB12LEVEL1Sub000001_01.textBox5.Text = m_ALDoors[04].ToString();
                    AB12LEVEL1Sub000001_01.textBox6.Text = m_ALDoors[05].ToString();
                    AB12LEVEL1Sub000001_01.textBox7.Text = m_ALDoors[06].ToString();
                    AB12LEVEL1Sub000001_01.textBox8.Text = m_ALDoors[07].ToString();
                    AB12LEVEL1Sub000001_01.textBox9.Text = m_ALDoors[08].ToString();
                    AB12LEVEL1Sub000001_01.textBox10.Text = m_ALDoors[09].ToString();
                    AB12LEVEL1Sub000001_01.textBox11.Text = m_ALDoors[10].ToString();
                    AB12LEVEL1Sub000001_01.textBox12.Text = m_ALDoors[11].ToString();
                }
                else
                {
                    AB4LEVEL4Sub000001_01.textBox1.Text = m_ALDoors[00].ToString();
                    AB4LEVEL4Sub000001_01.textBox2.Text = m_ALDoors[01].ToString();
                    AB4LEVEL4Sub000001_01.textBox3.Text = m_ALDoors[02].ToString();
                    AB4LEVEL4Sub000001_01.textBox4.Text = m_ALDoors[03].ToString();

                    AB4LEVEL3Sub000001_01.textBox1.Text = m_ALDoors[00].ToString();
                    AB4LEVEL3Sub000001_01.textBox2.Text = m_ALDoors[01].ToString();
                    AB4LEVEL3Sub000001_01.textBox3.Text = m_ALDoors[02].ToString();
                    AB4LEVEL3Sub000001_01.textBox4.Text = m_ALDoors[03].ToString();

                    AB4LEVEL2Sub000001_01.textBox1.Text = m_ALDoors[00].ToString();
                    AB4LEVEL2Sub000001_01.textBox2.Text = m_ALDoors[01].ToString();
                    AB4LEVEL2Sub000001_01.textBox3.Text = m_ALDoors[02].ToString();
                    AB4LEVEL2Sub000001_01.textBox4.Text = m_ALDoors[03].ToString();

                    AB4LEVEL1Sub000001_01.textBox1.Text = m_ALDoors[00].ToString();
                    AB4LEVEL1Sub000001_01.textBox2.Text = m_ALDoors[01].ToString();
                    AB4LEVEL1Sub000001_01.textBox3.Text = m_ALDoors[02].ToString();
                    AB4LEVEL1Sub000001_01.textBox4.Text = m_ALDoors[03].ToString();
                }
            }
            //--
            m_tabSub000001.Parent = m_tabMain;
            m_tabMain.SelectedTab = m_tabSub000001;

            //--
            //隱藏目前沒有功能的按鈕 at 2017/06/26
            butSub000001_08.Visible = false;
            butSub000001_09.Visible = false;
            butSub000001_10.Visible = false;
            butSub000001_11.Visible = false;
            //--
            //--
            //禁止再次編修項目
            cmbSub000001_01.Enabled = false;
            cmbSub000001_03.Enabled = false;
            labSub000001_09.Enabled = false;
            txtSub000001_04.Enabled = false;// add at 2017/08/07
            txtSub000001_03.Enabled = false;// add at 2017/08/07
            //--
            //--
            //butSub000001_12.Visible = true;
            //butSub000001_13.Visible = false;
            //--

            //--
            //add at 2017/10/05
            m_Sub000001ALInit.Clear();

            m_Sub000001ALInit.Add(txtSub000001_01.Text);//add at 2017/10/05
            m_Sub000001ALInit.Add(txtSub000001_02.Text);//add at 2017/10/05
            m_Sub000001ALInit.Add(labSub000001_09.Text);//add at 2017/10/05
            m_Sub000001ALInit.Add(txtSub000001_03.Text);//add at 2017/10/05
            m_Sub000001ALInit.Add(txtSub000001_04.Text);//add at 2017/10/05
            m_Sub000001ALInit.Add(rdbSub000001_01.Checked.ToString());//add at 2017/10/05
            m_Sub000001ALInit.Add(ckbSub000001_01.Checked.ToString());//add at 2017/10/05
            m_Sub000001ALInit.Add(rdbSub000001_03.Checked.ToString());//add at 2017/10/05
            m_Sub000001ALInit.Add(rdbSub000001_04.Checked.ToString());//add at 2017/10/05
            m_Sub000001ALInit.Add(ckbSub000001_02.Checked.ToString());//add at 2017/10/05
            m_Sub000001ALInit.Add(txtSub000001_19.Value + "");//add at 2017/10/05
            m_Sub000001ALInit.Add(cmbSub000001_01.SelectedIndex + "");//add at 2017/10/05
            m_Sub000001ALInit.Add(cmbSub000001_03.SelectedIndex + "");//add at 2017/10/05
            m_Sub000001ALInit.Add(cmbSub000001_04.SelectedIndex + "");//SYDM 選擇元件 修改偵測

            for (int i = 0; i < m_ALSub000001Date.Count; i++)//add at 2017/10/05
            {
                m_Sub000001ALInit.Add(m_ALSub000001Date[i].ToString());
            }

            for (int j = 0; j < m_ALDoors.Count; j++)//add at 2017/10/05
            {
                m_Sub000001ALInit.Add(m_ALDoors[j].ToString());
            }
            //--
            //--
            //add 2017/10/19
            labSub000001_03.ForeColor = Color.Black;
            labSub000001_01.ForeColor = Color.Black;
            labSub000001_04.ForeColor = Color.Black;
            labSub000001_20.ForeColor = Color.Black;//SYCG模式下新增控制器一定要有SYDM的防呆機制
            //--

            txtSub000001_05.Focus();
        }
        //---控制器UI多選編輯實作 ~ 把顯示控制器參數獨立成函數[setSub000001UI]

        private void butSub0000_02_Click(object sender, EventArgs e)//顯示新增控制器頁面
        {
            //---
            //控制器UI多選編輯實作 ~ 撰寫儲存查詢相關變數
            m_ALControllerObj.Clear();
            m_intControllerIndex = 0;
            //---控制器UI多選編輯實作 ~ 撰寫儲存查詢相關變數

            m_TPOld = m_tabMain.SelectedTab;//--2017/02/22 製作返回按鈕功能
            TabPage_Push();//m_StackTPOld.Push(m_TPOld);//--2017/02/22 製作返回按鈕功能
            initSub000001UI();//用來切換子元件顯示-2017/03/03

            m_intcontroller_sn = -1;//新增模式 所以沒有sn 2017/06/28

            m_tabSub000001.Parent = m_tabMain;
            m_tabMain.SelectedTab = m_tabSub000001;

            //--
            //隱藏目前沒有功能的按鈕 at 2017/06/26
            butSub000001_08.Visible = false;
            butSub000001_09.Visible = false;
            butSub000001_10.Visible = false;
            butSub000001_11.Visible = false;
            //--
            //--
            //因為和編修畫面相同，所以要做預防性的設定
            cmbSub000001_03.Enabled = true;
            labSub000001_09.Enabled = true;
            txtSub000001_04.Enabled = true;// add at 2017/08/07
            txtSub000001_03.Enabled = true;// add at 2017/08/07
            //--
            //--
            //butSub000001_12.Visible = true;
            //butSub000001_13.Visible = false;
            //--
            //--
            //add 2017/10/19
            labSub000001_03.ForeColor = Color.Black;
            labSub000001_01.ForeColor = Color.Black;
            labSub000001_04.ForeColor = Color.Black;
            labSub000001_20.ForeColor = Color.Black;//SYCG模式下新增控制器一定要有SYDM的防呆機制
            //--
        }
        private void butSub0000_07_Click(object sender, EventArgs e)//控制器啟用
        {
            ControllerBatchAction(0);
        }

        private void butSub0000_09_Click(object sender, EventArgs e)//控制器停用
        {
            ControllerBatchAction(1);
        }

        private void butSub0000_12_Click(object sender, EventArgs e)//控制器刪除
        {
            ControllerBatchAction(2);
        }

        public void ControllerBatchAction(int intstep)//控制器批量操作 //按照『V8 功能選單』一個一個改 - 控制器列表 ~ 下拉是選單 啟用/停用/刪除 控制器 變成獨立按鈕
        {
            ArrayList ALSN = new ArrayList();
            ArrayList ALIP = new ArrayList();
            ArrayList ALPort = new ArrayList();
            ArrayList ALMode = new ArrayList();
            ALSN.Clear();
            ALIP.Clear();
            ALPort.Clear();
            ALMode.Clear();

            //---
            //SYDM和SYCG API呼叫並存實現
            //HW_Net_API.setAPIDomainURL(txtSys_01.Text, txtSys_07.Text);
            if (!m_changeSYCGMode)//SYDM
            {
                HW_Net_API.setAPIDomainURL(txtSys_01.Text, txtSys_07.Text);
            }
            else//SYCG
            {
                HW_Net_API.SYCG_setSYCGDomainURL();
            }
            //---SYDM和SYCG API呼叫並存實現

            //---
            //SYDM和SYCG API呼叫並存實現
            if (!m_changeSYCGMode)//SYDM
            {
                m_blnAPI = HW_Net_API.getController_Connection();
            }
            else//SYCG
            {
                m_blnAPI = HW_Net_API.SYCG_getSYDMList();
                if (m_blnAPI)
                {
                    HW_Net_API.m_Controller_Connection.controllers.Clear();
                    for (int l = 0; l < m_Sydms.sydms.Count; l++)
                    {
                        HW_Net_API.SYCG_callSYDMCommand("SYDM_GET_CONTROLLER_CONNECTION", "", m_Sydms.sydms[l].identifier.ToString());
                    }
                }
            }
            //---SYDM和SYCG API呼叫並存實現	

            for (int i = 0; i < dgvSub0000_01.Rows.Count; i++)
            {
                String data = dgvSub0000_01.Rows[i].Cells[0].Value.ToString().ToLower();//抓取DataGridView欄位資料
                if (data == "true")
                {
                    ALSN.Add(dgvSub0000_01.Rows[i].Cells[4].Value.ToString());
                    ALIP.Add(dgvSub0000_01.Rows[i].Cells[5].Value.ToString());
                    ALPort.Add(dgvSub0000_01.Rows[i].Cells[6].Value.ToString());//修正控制器列表資訊排列順序 ALPort.Add(dgvSub0000_01.Rows[i].Cells[7].Value.ToString());
                    ALMode.Add(dgvSub0000_01.Rows[i].Cells[8].Value.ToString());
                }
            }
            String SQL = "";
            switch (intstep)//switch (cmbSub0000_01.SelectedIndex)
            {
                case 0:
                    for (int i = 0; i < ALSN.Count; i++)
                    {
                        int intsy_dm_Controller_id = -1;
                        SQL = "";
                        intsy_dm_Controller_id = Getsy_dm_Controller_id(ALMode[i].ToString(), ALIP[i].ToString(), ALPort[i].ToString());//修正Getsy_dm_Controller_id 函數和相關呼叫點-intsy_dm_Controller_id = Getsy_dm_Controller_id(ALSN[i].ToString(), ALIP[i].ToString(), ALPort[i].ToString());
                        if (intsy_dm_Controller_id > 0)
                        {
                            CC_Controller CC_data = new CC_Controller();
                            CC_data.connection = new CC_Connection();
                            CC_data.identifier = intsy_dm_Controller_id;
                            CC_data.connection.enabled = 1;
                            CC_data.connection.mode = Convert.ToInt32(ALMode[i].ToString());
                            CC_data.connection.port = Convert.ToInt32(ALPort[i].ToString());
                            CC_data.connection.address = HW_Net_API.ip2long(ALIP[i].ToString(), true);//IP //修正所有API內有關IP的運算公式變成32位元版-允許負數 //把IP轉換函數從32位元版改回64位元版-不允許有負數
                            CC_data.connection.serial_number = Convert.ToInt64(ALSN[i].ToString(), 16);//十六進位轉十進位

                            //---
                            //SYDM和SYCG API呼叫並存實現
                            if (!m_changeSYCGMode)//SYDM
                            {
                                m_blnAPI = HW_Net_API.setController_Connection(CC_data);
                            }
                            else//SYCG
                            {
                                //---
                                //SYCG模式下-建立/暫存 當下要操作的SYDM ID
                                SQL = String.Format("SELECT c.sydm_id AS sydm_id FROM controller AS c WHERE (c.sn='{0}')", ALSN[i].ToString());
                                MySqlDataReader Readerd_SYDMid = MySQL.GetDataReader(SQL);
                                while (Readerd_SYDMid.Read())
                                {
                                    m_intSYDM_id = Convert.ToInt32(Readerd_SYDMid["sydm_id"].ToString());//SYCG模式下-建立/暫存 當下要操作的SYDM ID
                                    break;
                                }
                                Readerd_SYDMid.Close();
                                //---SYCG模式下-建立/暫存 當下要操作的SYDM ID
                                String StrCC_buf = parseJSON.composeJSON_Controller_Connection(CC_data);
                                m_blnAPI = HW_Net_API.SYCG_callSYDMCommand("SYDM_SET_CONTROLLER_CONNECTION", StrCC_buf, m_intSYDM_id.ToString());
                            }
                            //---SYDM和SYCG API呼叫並存實現	
                            if (m_blnAPI)//if (HW_Net_API.setController_Connection(CC_data))
                            {
                                SQL += "UPDATE controller_extend SET connetction_enabled = 1,state=0 WHERE (controller_sn = " + ALSN[i].ToString() + ");";//modified at 2017/06/29
                            }
                            else
                            {
                                SQL += "UPDATE controller_extend SET connetction_enabled = 1,state=1 WHERE (controller_sn = " + ALSN[i].ToString() + ");";//modified at 2017/06/29
                            }
                        }
                        else
                        {
                            SQL += "UPDATE controller_extend SET connetction_enabled = 1,state=1 WHERE (controller_sn = " + ALSN[i].ToString() + ");";//modified at 2017/06/29
                        }
                        MySQL.InsertUpdateDelete(SQL);//新增資料程式
                    }
                    //enableSelectsdgvSub0000_01();
                    break;
                case 1:
                    for (int i = 0; i < ALSN.Count; i++)
                    {
                        int intsy_dm_Controller_id = -1;
                        SQL = "";
                        intsy_dm_Controller_id = Getsy_dm_Controller_id(ALMode[i].ToString(), ALIP[i].ToString(), ALPort[i].ToString());//修正Getsy_dm_Controller_id 函數和相關呼叫點-intsy_dm_Controller_id = Getsy_dm_Controller_id(ALSN[i].ToString(), ALIP[i].ToString(), ALPort[i].ToString());
                        if (intsy_dm_Controller_id > 0)
                        {
                            CC_Controller CC_data = new CC_Controller();
                            CC_data.connection = new CC_Connection();
                            CC_data.identifier = intsy_dm_Controller_id;
                            CC_data.connection.enabled = 0;
                            CC_data.connection.mode = Convert.ToInt32(ALMode[i].ToString());
                            CC_data.connection.port = Convert.ToInt32(ALPort[i].ToString());
                            CC_data.connection.address = HW_Net_API.ip2long(ALIP[i].ToString(), true);//IP //修正所有API內有關IP的運算公式變成32位元版-允許負數 //把IP轉換函數從32位元版改回64位元版-不允許有負數
                            CC_data.connection.serial_number = Convert.ToInt64(ALSN[i].ToString(), 16);//十六進位轉十進位

                            //---
                            //SYDM和SYCG API呼叫並存實現
                            if (!m_changeSYCGMode)//SYDM
                            {
                                m_blnAPI = HW_Net_API.setController_Connection(CC_data);
                            }
                            else//SYCG
                            {
                                //---
                                //SYCG模式下-建立/暫存 當下要操作的SYDM ID
                                SQL = String.Format("SELECT c.sydm_id AS sydm_id FROM controller AS c WHERE (c.sn='{0}')", ALSN[i].ToString());
                                MySqlDataReader Readerd_SYDMid = MySQL.GetDataReader(SQL);
                                while (Readerd_SYDMid.Read())
                                {
                                    m_intSYDM_id = Convert.ToInt32(Readerd_SYDMid["sydm_id"].ToString());//SYCG模式下-建立/暫存 當下要操作的SYDM ID
                                    break;
                                }
                                Readerd_SYDMid.Close();
                                //---SYCG模式下-建立/暫存 當下要操作的SYDM ID
                                String StrCC_buf = parseJSON.composeJSON_Controller_Connection(CC_data);
                                m_blnAPI = HW_Net_API.SYCG_callSYDMCommand("SYDM_SET_CONTROLLER_CONNECTION", StrCC_buf, m_intSYDM_id.ToString());
                            }
                            //---SYDM和SYCG API呼叫並存實現	
                            if (m_blnAPI)//if (HW_Net_API.setController_Connection(CC_data))
                            {
                                SQL += "UPDATE controller_extend SET connetction_enabled = 0,state=0 WHERE (controller_sn = " + ALSN[i].ToString() + ");";//modified at 2017/06/29
                            }
                            else
                            {
                                SQL += "UPDATE controller_extend SET connetction_enabled = 0,state=1 WHERE (controller_sn = " + ALSN[i].ToString() + ");";//modified at 2017/06/29
                            }
                        }
                        else
                        {
                            SQL += "UPDATE controller_extend SET connetction_enabled = 0,state=1 WHERE (controller_sn = " + ALSN[i].ToString() + ");";//modified at 2017/06/29
                        }
                        //SQL = "";
                        //SQL += "UPDATE controller_extend SET connetction_enabled = 0,state=1 WHERE (controller_sn = " + ALSN[i].ToString() + ");";//modified at 2017/06/29
                        MySQL.InsertUpdateDelete(SQL);//新增資料程式
                    }
                    //disableSelectsdgvSub0000_01();
                    break;
                case 2:
                    for (int i = 0; i < ALSN.Count; i++)
                    {
                        int intsy_dm_Controller_id = -1;
                        SQL = "";
                        intsy_dm_Controller_id = Getsy_dm_Controller_id(ALMode[i].ToString(), ALIP[i].ToString(), ALPort[i].ToString());//修正Getsy_dm_Controller_id 函數和相關呼叫點-intsy_dm_Controller_id = Getsy_dm_Controller_id(ALSN[i].ToString(), ALIP[i].ToString(), ALPort[i].ToString());
                        if (intsy_dm_Controller_id > 0)
                        {
                            CA_Controller CA_data = new CA_Controller();
                            CA_data.identifier = intsy_dm_Controller_id;
                            CA_data.active = 0;

                            //---
                            //SYDM和SYCG API呼叫並存實現
                            if (!m_changeSYCGMode)//SYDM
                            {
                                m_blnAPI = HW_Net_API.setController_Active(CA_data);
                            }
                            else//SYCG
                            {
                                String StrCA_buf = parseJSON.composeJSON_Controller_Active(CA_data);
                                m_blnAPI = HW_Net_API.SYCG_callSYDMCommand("SYDM_SET_CONTROLLER_ACTIVE", StrCA_buf, m_intSYDM_id.ToString());
                            }
                            //---SYDM和SYCG API呼叫並存實現
                            if (m_blnAPI)//if (HW_Net_API.setController_Active(CA_data))
                            {
                                SQL += String.Format("DELETE FROM controller WHERE sn={0};DELETE FROM controller_extend WHERE controller_sn={0};DELETE FROM door WHERE controller_id={0};", ALSN[i].ToString());
                            }
                        }
                        else
                        {//允許未匯到sy_dm但設定停用時可刪除
                            bool blnHaveData = false;
                            SQL = String.Format("SELECT controller_sn FROM controller_extend WHERE connetction_enabled=0 AND controller_sn={0} AND state!=-1;", ALSN[i].ToString());//多了 AND state!=-1 ->修正未在線的控制器匯入無法顯示問題
                            MySqlDataReader Reader_Date = MySQL.GetDataReader(SQL);
                            while (Reader_Date.Read())
                            {
                                blnHaveData = true;
                                break;
                            }
                            Reader_Date.Close();
                            if (blnHaveData)
                            {
                                SQL += String.Format("DELETE FROM controller WHERE sn={0};DELETE FROM controller_extend WHERE controller_sn={0};DELETE FROM door WHERE controller_id={0};", ALSN[i].ToString());
                            }
                        }
                        if (SQL.Length > 0)
                        {
                            MySQL.InsertUpdateDelete(SQL);//新增資料程式
                        }
                    }
                    break;
            }

            //---
            //SYDM和SYCG API呼叫並存實現
            if (!m_changeSYCGMode)//SYDM
            {
                m_blnAPI = HW_Net_API.load_All_Controller();//重載控制器
            }
            else//SYCG
            {
                m_blnAPI = HW_Net_API.SYCG_callSYDMCommand("SYDM_LOAD_CONTROLLER", "", m_intSYDM_id.ToString());
            }
            //---SYDM和SYCG API呼叫並存實現
            get_show_Controllers(false);
        }

        public bool m_blnCSelectAll = true;//按照『V8 功能選單』一個一個改 - 控制器列表 ~ 全選/取消全選 整合成同一個按鈕
        private void butSub0000_08_Click(object sender, EventArgs e)//全選+取消全選
        {
            /*
            for (int i = 0; i < dgvSub0000_01.Rows.Count; i++)
            {
                dgvSub0000_01.Rows[i].Cells[0].Value = false;
                dgvSub0000_01.Rows[i].Selected = false;
            }
            */
            //---
            //按照『V8 功能選單』一個一個改 - 控制器列表 ~ 全選/取消全選 整合成同一個按鈕

            //dgvSub0000_01.ClearSelection();//把所有列表元件的全選/取消全選改成SelectAll()/ClearSelection()
            if (m_blnCSelectAll == true)
            {
                dgvSub0000_01.SelectAll();//把所有列表元件的全選/取消全選改成SelectAll()/ClearSelection()
                butSub0000_08.ImageIndex = 7;
                butSub0000_08.Text = Language.m_StrbutSub0000_08_02;
            }
            else
            {
                dgvSub0000_01.ClearSelection();//把所有列表元件的全選/取消全選改成SelectAll()/ClearSelection()
                butSub0000_08.ImageIndex = 8;
                butSub0000_08.Text = Language.m_StrbutSub0000_08_01;
            }
            m_blnCSelectAll = (!m_blnCSelectAll);

            //---按照『V8 功能選單』一個一個改 - 控制器列表 ~ 全選/取消全選 整合成同一個按鈕
        }
        public int m_intdgvSub0000_01_SN = 0;//控制器列表的 DB index
        private void dgvSub0000_01_SelectionChanged(object sender, EventArgs e)//控制器列表 DataGridView取DB index at 2017/05/22 20:47
        {
            //---
            //列表元件shift全選時最前面的狀態要能自動打勾，反之要能取消
            for (int i = 0; i < dgvSub0000_01.Rows.Count; i++)
            {
                dgvSub0000_01.Rows[i].Cells[0].Value = false;
            }
            for (int j = 0; j < dgvSub0000_01.SelectedRows.Count; j++)
            {
                dgvSub0000_01.SelectedRows[j].Cells[0].Value = true;
            }
            //---列表元件shift全選時最前面的狀態要能自動打勾，反之要能取消

            try
            {
                int index = dgvSub0000_01.SelectedRows[0].Index;//取得被選取的第一列位置
                String Strsn = dgvSub0000_01.Rows[index].Cells[4].Value.ToString();
                m_intdgvSub0000_01_SN = Int32.Parse(Strsn.Replace("unknown-", ""));//控制器列表的unknown列SN也要加上unknown-
                //MessageBox.Show(Strid);
            }
            catch
            {
            }
        }
        /*
        public void enableSelectsdgvSub0000_01()//啟用選擇
        {
            for (int i = 0; i < dgvSub0000_01.Rows.Count; i++)
            {
                String data = dgvSub0000_01.Rows[i].Cells[0].Value.ToString().ToLower();//抓取DataGridView欄位資料
                if (data == "true")
                {
                    dgvSub0000_01.Rows[i].Cells[6].Value = "Enable";
                }
            }
        }
        public void disableSelectsdgvSub0000_01()//停用選擇
        {
            for (int i = 0; i < dgvSub0000_01.Rows.Count; i++)
            {
                String data = dgvSub0000_01.Rows[i].Cells[0].Value.ToString().ToLower();//抓取DataGridView欄位資料
                if (data == "true")
                {
                    dgvSub0000_01.Rows[i].Cells[6].Value = "Disable";
                }
            }
        }
        */
 
        public String m_SQLcondition01 = "";
        public String m_SQLcondition02 = "";
        public String m_SQLcondition03 = "";
        public String m_SQLcondition04 = "";
        public String m_SQLcondition05 = "";
        public String m_SQLcondition06 = "";
        private void ckbSub0000_01_CheckedChanged(object sender, EventArgs e)//控制器列表~已啟用控制器過濾
        {
            if (((CheckBox)sender).Checked == false)
            {
                m_SQLcondition01 = "";
                if (ckbSub0000_02.Checked == true)
                {
                    m_SQLcondition02 = " AND ";
                    m_SQLcondition02 += "(c_e.connetction_enabled=0)";//modified at 2017/06/29
                }
                else
                {
                    m_SQLcondition02 = "";
                }
            }
            else
            {
                if (ckbSub0000_02.Checked != true)
                {
                    m_SQLcondition01 = " AND ";
                    m_SQLcondition01 += "(c_e.connetction_enabled=1)";//modified at 2017/06/29
                    m_SQLcondition02 = "";
                }
                else
                {
                    m_SQLcondition01 = " AND ";
                    m_SQLcondition01 += "((c_e.connetction_enabled=1) OR (c_e.connetction_enabled=0) )";//modified at 2017/06/29
                    m_SQLcondition02 = "";
                    m_SQLcondition02 = "";
                }

            }
            get_show_Controllers(false);//控制器過濾器要有作用
        }

        private void ckbSub0000_02_CheckedChanged(object sender, EventArgs e)//控制器列表~已停用控制器過濾
        {
            if (((CheckBox)sender).Checked == false)
            {
                m_SQLcondition02 = "";
                if (ckbSub0000_01.Checked == true)
                {
                    m_SQLcondition01 = " AND ";
                    m_SQLcondition01 += "(c_e.connetction_enabled=0)";//modified at 2017/06/29
                }
                else
                {
                    m_SQLcondition01 = "";
                }
            }
            else
            {
                if (ckbSub0000_01.Checked != true)
                {
                    m_SQLcondition02 = " AND ";
                    m_SQLcondition02 += "(c_e.connetction_enabled=0)";//modified at 2017/06/29
                    m_SQLcondition01 = "";
                }
                else
                {
                    m_SQLcondition02 = " AND ";
                    m_SQLcondition02 += "((c_e.connetction_enabled=1) OR (c_e.connetction_enabled=0) )";//modified at 2017/06/29
                    m_SQLcondition01 = "";
                    m_SQLcondition01 = "";
                }
            }
            get_show_Controllers(false);//控制器過濾器要有作用
        }

        private void ckbSub0000_0304_CheckedChanged(object sender, EventArgs e)//控制器列表~門禁控制器過濾+控制器列表~電梯控制器過濾
        {
            //--
            //實作控制器列表過濾電梯和門禁控制器功能
            m_SQLcondition04 = "";
            if (ckbSub0000_03.Checked == false)
            {//不要顯示門禁
                m_SQLcondition03 = "";
                if (ckbSub0000_04.Checked == false)
                {//都要顯示
                    m_SQLcondition03 = "";
                }
                else
                {//只顯示電梯
                    m_SQLcondition03 = "AND (m.model=5)";
                }
            }
            else
            {//要顯示門禁
                m_SQLcondition03 = "";
                if (ckbSub0000_04.Checked == false)
                {//都要顯示
                    m_SQLcondition03 = "";
                }
                else
                {//只顯示門禁
                    m_SQLcondition03 = "AND (m.model<5)";
                }
            }
            //--
            get_show_Controllers(false);//控制器過濾器要有作用

        }

        private void ckbSub0000_05_CheckedChanged(object sender, EventArgs e)//控制器列表~已啟用A.P.B.過濾
        {
            if (((CheckBox)sender).Checked == false)
            {
                m_SQLcondition05 = "";
            }
            else
            {
                m_SQLcondition05 = " AND ";
                m_SQLcondition05 += "(c_e.apb_enable=1)";//modified at 2017/06/29
                
            }
            get_show_Controllers(false);//控制器過濾器要有作用
        }

        private void ckbSub0000_06_CheckedChanged(object sender, EventArgs e)//控制器列表~已啟用A/B門過濾
        {
            if (((CheckBox)sender).Checked == false)
            {
                m_SQLcondition06 = "";
                
            }
            else
            {
                m_SQLcondition06 = " AND ";
                m_SQLcondition06 += "(c_e.ab_door_enabled=1)";//modified at 2017/06/29
            }
            get_show_Controllers(false);//控制器過濾器要有作用
        }

        private void butSub0000_10_Click(object sender, EventArgs e)//控制器列表~搜尋
        {
            get_show_Controllers();

            ArrayList AL01 = new ArrayList();
            ArrayList AL02 = new ArrayList();
            ArrayList AL03 = new ArrayList();
            ArrayList AL04 = new ArrayList();
            ArrayList AL05 = new ArrayList();
            ArrayList AL06 = new ArrayList();
            ArrayList AL07 = new ArrayList();// add 2017/08/23
            ArrayList AL08 = new ArrayList();// add 2017/08/23
            ArrayList AL09 = new ArrayList();// add 2017/08/23
            AL01.Clear();
            AL02.Clear();
            AL03.Clear();
            AL04.Clear();
            AL05.Clear();
            AL06.Clear();
            AL07.Clear();// add 2017/08/23
            AL08.Clear();// add 2017/08/23
            AL09.Clear();// add 2017/08/23

            if (txtSub0000_01.Text != "")
            {
                for (int i = 0; i < dgvSub0000_01.Rows.Count; i++)//取的現行UI上控制器列表所有資料
                {
                    AL01.Add(dgvSub0000_01.Rows[i].Cells[1].Value.ToString());
                    AL02.Add(dgvSub0000_01.Rows[i].Cells[2].Value.ToString());
                    AL03.Add(dgvSub0000_01.Rows[i].Cells[3].Value.ToString());
                    AL04.Add(dgvSub0000_01.Rows[i].Cells[4].Value.ToString());
                    AL05.Add(dgvSub0000_01.Rows[i].Cells[5].Value.ToString());
                    AL06.Add(dgvSub0000_01.Rows[i].Cells[6].Value.ToString());
                    AL07.Add(dgvSub0000_01.Rows[i].Cells[7].Value.ToString());// add 2017/08/23
                    AL08.Add(dgvSub0000_01.Rows[i].Cells[8].Value.ToString());// add 2017/08/23
                    AL09.Add(dgvSub0000_01.Rows[i].Cells[9].Value);// add 2017/08/23
                }

                try//清空控制器列表UI上所有資料
                {
                    //--
                    //dgvSub000001_01.ReadOnly = true;//唯讀 不可更改
                    dgvSub0000_01.RowHeadersVisible = false;//DataGridView 最前面指示選取列所在位置的箭頭欄位
                    dgvSub0000_01.Rows[0].Selected = false;//取消DataGridView的默認選取(選中)Cell 使其不反藍
                    dgvSub0000_01.AllowUserToAddRows = false;//是否允許使用者新增資料
                    dgvSub0000_01.AllowUserToDeleteRows = false;//是否允許使用者刪除資料
                    dgvSub0000_01.AllowUserToOrderColumns = false;//是否允許使用者調整欄位位置
                    //所有表格欄位寬度全部變成可調 dgvSub0000_01.AllowUserToResizeColumns = false;//是否允許使用者改變欄寬
                    dgvSub0000_01.AllowUserToResizeRows = false;//是否允許使用者改變行高
                    dgvSub0000_01.Columns[1].ReadOnly = true;//單一欄位禁止編輯
                    dgvSub0000_01.Columns[2].ReadOnly = true;//單一欄位禁止編輯
                    dgvSub0000_01.Columns[3].ReadOnly = true;//單一欄位禁止編輯
                    dgvSub0000_01.Columns[4].ReadOnly = true;//單一欄位禁止編輯
                    dgvSub0000_01.Columns[5].ReadOnly = true;//單一欄位禁止編輯
                    dgvSub0000_01.Columns[6].ReadOnly = true;//單一欄位禁止編輯
                    dgvSub0000_01.AllowUserToAddRows = false;//刪除空白列
                    dgvSub0000_01.SelectionMode = System.Windows.Forms.DataGridViewSelectionMode.FullRowSelect;//整列選取
                    //--

                    do
                    {
                        for (int i = 0; i < dgvSub0000_01.Rows.Count; i++)
                        {
                            DataGridViewRow r1 = this.dgvSub0000_01.Rows[i];//取得DataGridView整列資料
                            this.dgvSub0000_01.Rows.Remove(r1);//DataGridView刪除整列
                        }
                    } while (dgvSub0000_01.Rows.Count > 0);

                }
                catch
                {
                }
                String StrSearch = txtSub0000_01.Text;
                for(int i=0;i<AL01.Count;i++)
                {
                    //AL01[i].ToString()->DB index 本來就被隱藏 所以不用在搜尋欄位內
                    if ( (AL02[i].ToString().IndexOf(StrSearch) > -1) || (AL03[i].ToString().IndexOf(StrSearch) > -1) || (AL04[i].ToString().IndexOf(StrSearch) > -1) || (AL05[i].ToString().IndexOf(StrSearch) > -1) || (AL06[i].ToString().IndexOf(StrSearch) > -1) )
                    {
                        this.dgvSub0000_01.Rows.Add(false, AL01[i].ToString(), AL02[i].ToString(), AL03[i].ToString(), AL04[i].ToString(), AL05[i].ToString(), AL06[i].ToString(), AL07[i].ToString(), AL08[i].ToString(), ((Image)AL09[i]));
                    }
                }
            }
        }
        private void butSub0000_03_Click(object sender, EventArgs e)//匯入控制器
        {
            butSub0000_03.Enabled = false;

            //---
            //SYDM和SYCG API呼叫並存實現
            //HW_Net_API.setAPIDomainURL(txtSys_01.Text, txtSys_07.Text);
            if (!m_changeSYCGMode)//SYDM
            {
                HW_Net_API.setAPIDomainURL(txtSys_01.Text, txtSys_07.Text);
            }
            else//SYCG
            {
                HW_Net_API.SYCG_setSYCGDomainURL();
            }
            //---SYDM和SYCG API呼叫並存實現

            Animation.createThreadAnimation(butSub0000_03.Text, Animation.Thread_importControllers);//重寫匯入控制器按鈕變成有等待動畫程式

            get_show_Controllers(false);//取的控制器列表

            butSub0000_03.Enabled = true;
        }

        private void butSub0000_04_Click(object sender, EventArgs e)//匯出控制器
        {
            for (int i = 0; i < dgvSub0000_01.Rows.Count; i++)
            {
                if (dgvSub0000_01.Rows[i].Cells[0].Value.ToString().ToLower() == "true")
                {
                    String Strid = dgvSub0000_01.Rows[i].Cells[1].Value.ToString();

                }
            }
        }

        private void butSub0000_11_Click(object sender, EventArgs e)//更新控制器列表狀態
        {
            butSub0000_11.Enabled = false;
            get_show_Controllers(false);//取的控制器列表
            butSub0000_11.Enabled = true;
        }

        private void dgvSub0000_01_CellMouseMove(object sender, DataGridViewCellMouseEventArgs e)//顯示控制器狀態燈號說明Tip
        {
            if (e.ColumnIndex == 9)
            {
                String StrshowMsg = Language.m_StrdgvSub0000_01Tip00 + "\n" + Language.m_StrdgvSub0000_01Tip01 + "\n" + Language.m_StrdgvSub0000_01Tip02;
                if (!TooltipToolV2.blnRun)
                {
                    butSub0000_10.ShowTooltip(toolTip1, StrshowMsg, 0, 0);
                }
            }
        }
        private void dgvSub0000_01_CellMouseLeave(object sender, DataGridViewCellEventArgs e)//顯示控制器狀態燈號說明Tip
        {
            if (e.ColumnIndex == 9)
            {
                toolTip1.Hide(butSub0000_10);
                TooltipToolV2.blnRun = false;
            }
        }
        
        //--Sub0000_end
        //Sub0003_start
        private void dgvSub0003_01_DoubleClick(object sender, EventArgs e)//at 2017/09/25
        {
            butSub0003_01.PerformClick();
        }
        public int m_intdgvSub0003_01_id = -1;
        private void dgvSub0003_01_SelectionChanged(object sender, EventArgs e)
        {
            //---
            //列表元件shift全選時最前面的狀態要能自動打勾，反之要能取消
            for (int i = 0; i < dgvSub0003_01.Rows.Count; i++)
            {
                dgvSub0003_01.Rows[i].Cells[0].Value = false;
            }
            for (int j = 0; j < dgvSub0003_01.SelectedRows.Count; j++)
            {
                dgvSub0003_01.SelectedRows[j].Cells[0].Value = true;
            }
            //---列表元件shift全選時最前面的狀態要能自動打勾，反之要能取消

            try
            {
                int index = dgvSub0003_01.SelectedRows[0].Index;//取得被選取的第一列位置
                String Strid = dgvSub0003_01.Rows[index].Cells[1].Value.ToString();
                m_intdgvSub0003_01_id = Int32.Parse(Strid);
            }
            catch
            {
                m_intdgvSub0003_01_id = -1;
            }
        }

        private void ckbSub0003_01_CheckedChanged(object sender, EventArgs e)//A.P.B已啟用/已停用 篩選事件
        {
            m_StrdgvSub0003_01_ext01 = "";
            if (ckbSub0003_01.Checked)
            {
                if (ckbSub0003_02.Checked)//(1,1)
                {
                    m_StrdgvSub0003_01_ext01 = "";//兩個都選等於沒選
                }
                else//(1,0)
                {
                    m_StrdgvSub0003_01_ext01 = " WHERE status = 1";
                }
            }
            else
            {
                if (ckbSub0003_02.Checked)//(0,1)
                {
                    m_StrdgvSub0003_01_ext01 = " WHERE status = 0";
                }
                else//(0,0)
                {
                    m_StrdgvSub0003_01_ext01 = "";//沒選
                }
            }
            initdgvSub0003_01();
        }

        private void butSub0003_10_Click(object sender, EventArgs e)//A.P.B搜尋
        {
            initdgvSub0003_01();
            ArrayList AL01 = new ArrayList();
            ArrayList AL02 = new ArrayList();
            ArrayList AL03 = new ArrayList();
            ArrayList AL04 = new ArrayList();
            ArrayList AL05 = new ArrayList();
            AL01.Clear();
            AL02.Clear();
            AL03.Clear();
            AL04.Clear();
            AL05.Clear();


            if (txtSub0003_01.Text != "")
            {
                for (int i = 0; i < dgvSub0003_01.Rows.Count; i++)//取的現行UI上控制器列表所有資料
                {
                    AL01.Add(dgvSub0003_01.Rows[i].Cells[1].Value.ToString());
                    AL02.Add(dgvSub0003_01.Rows[i].Cells[2].Value.ToString());
                    AL03.Add(dgvSub0003_01.Rows[i].Cells[3].Value.ToString());
                    AL04.Add(dgvSub0003_01.Rows[i].Cells[4].Value.ToString());
                    AL05.Add(dgvSub0003_01.Rows[i].Cells[5].Value.ToString());
                }
                }
                try
                {
                    //--
                    //dgvSub0003_01.ReadOnly = true;//唯讀 不可更改
                    dgvSub0003_01.RowHeadersVisible = false;//DataGridView 最前面指示選取列所在位置的箭頭欄位
                    dgvSub0003_01.Rows[0].Selected = false;//取消DataGridView的默認選取(選中)Cell 使其不反藍
                    dgvSub0003_01.AllowUserToAddRows = false;//是否允許使用者新增資料
                    dgvSub0003_01.AllowUserToDeleteRows = false;//是否允許使用者刪除資料
                    dgvSub0003_01.AllowUserToOrderColumns = false;//是否允許使用者調整欄位位置
                    //所有表格欄位寬度全部變成可調 dgvSub0003_01.AllowUserToResizeColumns = false;//是否允許使用者改變欄寬
                    dgvSub0003_01.AllowUserToResizeRows = false;//是否允許使用者改變行高
                    dgvSub0003_01.Columns[1].ReadOnly = true;//單一欄位禁止編輯
                    dgvSub0003_01.Columns[2].ReadOnly = true;//單一欄位禁止編輯
                    dgvSub0003_01.Columns[3].ReadOnly = true;//單一欄位禁止編輯
                    dgvSub0003_01.Columns[4].ReadOnly = true;//單一欄位禁止編輯
                    dgvSub0003_01.Columns[5].ReadOnly = true;//單一欄位禁止編輯
                    dgvSub0003_01.AllowUserToAddRows = false;//刪除空白列
                    dgvSub0003_01.SelectionMode = System.Windows.Forms.DataGridViewSelectionMode.FullRowSelect;//整列選取
                    //--

                    do
                    {
                        for (int i = 0; i < dgvSub0003_01.Rows.Count; i++)
                        {
                            DataGridViewRow r1 = this.dgvSub0003_01.Rows[i];//取得DataGridView整列資料
                            this.dgvSub0003_01.Rows.Remove(r1);//DataGridView刪除整列
                        }
                    } while (dgvSub0003_01.Rows.Count > 0);

                }
                catch
                {
                }
                String StrSearch = txtSub0003_01.Text;
                for (int i = 0; i < AL01.Count; i++)
                {
                    //AL01[i].ToString()->DB index 本來就被隱藏 所以不用在搜尋欄位內
                    if ((AL02[i].ToString().IndexOf(StrSearch) > -1) || (AL03[i].ToString().IndexOf(StrSearch) > -1) || (AL04[i].ToString().IndexOf(StrSearch) > -1) || (AL05[i].ToString().IndexOf(StrSearch) > -1))
                    {
                        this.dgvSub0003_01.Rows.Add(false, AL01[i].ToString(), AL02[i].ToString(), AL03[i].ToString(), AL04[i].ToString(), AL05[i].ToString());
                    }
                }
            }
        }

        private void butSub0003_07_Click(object sender, EventArgs e)//A.P.B啟用
        {
            //---
            //按照『V8 功能選單』一個一個改 - 門區APB群組 ~ 下拉是選單 啟用/停用/刪除 控制器 變成獨立按鈕
            /*
            for (int i = 0; i < dgvSub0003_01.Rows.Count; i++)
            {
                dgvSub0003_01.Rows[i].Cells[0].Value = true;
                dgvSub0003_01.Rows[i].Selected = true;
            }
            */
            //dgvSub0003_01.SelectAll();//把所有列表元件的全選/取消全選改成SelectAll()/ClearSelection()
            APBBatchAction(0);
            //---按照『V8 功能選單』一個一個改 - 門區APB群組 ~ 下拉是選單 啟用/停用/刪除 控制器 變成獨立按鈕
        }

        private void butSub0003_11_Click(object sender, EventArgs e)//A.P.B停用
        {
            //---
            //按照『V8 功能選單』一個一個改 - 門區APB群組 ~ 下拉是選單 啟用/停用/刪除 控制器 變成獨立按鈕
            APBBatchAction(1);
            //---按照『V8 功能選單』一個一個改 - 門區APB群組 ~ 下拉是選單 啟用/停用/刪除 控制器 變成獨立按鈕
        }

        private void butSub0003_09_Click(object sender, EventArgs e)//A.P.B刪除
        {
            //---
            //按照『V8 功能選單』一個一個改 - 門區APB群組 ~ 下拉是選單 啟用/停用/刪除 控制器 變成獨立按鈕
            /*
            ArrayList ALSN = new ArrayList();
            ALSN.Clear();
            for (int i = 0; i < dgvSub0003_01.Rows.Count; i++)
            {
                String data = dgvSub0003_01.Rows[i].Cells[0].Value.ToString().ToLower();//抓取DataGridView欄位資料
                if (data == "true")
                {
                    ALSN.Add(dgvSub0003_01.Rows[i].Cells[1].Value.ToString());//抓 ID
                }
            }
            String SQL = "";
            switch (cmbSub0003_01.SelectedIndex)
            {
                case 0:
                    SQL = "";
                    for (int i = 0; i < ALSN.Count; i++)
                    {
                        SQL += "UPDATE apb_group SET status = 1,state=1 WHERE (id = " + ALSN[i].ToString() + ");";
                    }

                    break;
                case 1:
                    SQL = "";
                    for (int i = 0; i < ALSN.Count; i++)
                    {
                        SQL += "UPDATE apb_group SET status = 0,state=1 WHERE (id = " + ALSN[i].ToString() + ");";
                    }
                    break;
                case 2:
                    SQL = "";
                    for (int i = 0; i < ALSN.Count; i++)
                    {
                        SQL += String.Format("DELETE FROM apb_group WHERE id={0};DELETE FROM apb_group_extend WHERE apb_group_id={0};DELETE FROM apb_door WHERE apb_group_id={0};", ALSN[i].ToString());
                    }
                    break;
            }
            MySQL.InsertUpdateDelete(SQL);//新增資料程式

            initdgvSub0003_01();
            */
            APBBatchAction(2);
            //---按照『V8 功能選單』一個一個改 - 門區APB群組 ~ 下拉是選單 啟用/停用/刪除 控制器 變成獨立按鈕
        }

        public void APBBatchAction(int step)
        {
            ArrayList ALSN = new ArrayList();
            ALSN.Clear();
            for (int i = 0; i < dgvSub0003_01.Rows.Count; i++)
            {
                String data = dgvSub0003_01.Rows[i].Cells[0].Value.ToString().ToLower();//抓取DataGridView欄位資料
                if (data == "true")
                {
                    ALSN.Add(dgvSub0003_01.Rows[i].Cells[1].Value.ToString());//抓 ID
                }
            }
            String SQL = "";
            switch (step)
            {
                case 0:
                    SQL = "";
                    for (int i = 0; i < ALSN.Count; i++)
                    {
                        SQL += "UPDATE apb_group SET status = 1,state=1 WHERE (id = " + ALSN[i].ToString() + ");";
                    }

                    break;
                case 1:
                    SQL = "";
                    for (int i = 0; i < ALSN.Count; i++)
                    {
                        SQL += "UPDATE apb_group SET status = 0,state=1 WHERE (id = " + ALSN[i].ToString() + ");";
                    }
                    break;
                case 2:
                    SQL = "";
                    for (int i = 0; i < ALSN.Count; i++)
                    {
                        SQL += String.Format("DELETE FROM apb_group WHERE id={0};DELETE FROM apb_group_extend WHERE apb_group_id={0};DELETE FROM apb_door WHERE apb_group_id={0};", ALSN[i].ToString());
                    }
                    break;
            }
            MySQL.InsertUpdateDelete(SQL);//新增資料程式

            initdgvSub0003_01();
        }

        public bool m_blnApbSelectAll = true;//按照『V8 功能選單』一個一個改 - 門區APB群組 ~ 全選/取消全選 整合成同一個按鈕
        private void butSub0003_08_Click(object sender, EventArgs e)//按照『V8 功能選單』一個一個改 - 門區APB群組 ~ 全選/取消全選 整合成同一個按鈕
        {
            /*
            for (int i = 0; i < dgvSub0003_01.Rows.Count; i++)
            {
                dgvSub0003_01.Rows[i].Cells[0].Value = false;
                dgvSub0003_01.Rows[i].Selected = false;
            }
            */
            //---
            //按照『V8 功能選單』一個一個改 - 門區APB群組 ~ 全選/取消全選 整合成同一個按鈕
            //dgvSub0003_01.ClearSelection();//把所有列表元件的全選/取消全選改成SelectAll()/ClearSelection()
            if (m_blnApbSelectAll == true)
            {
                dgvSub0003_01.SelectAll();//把所有列表元件的全選/取消全選改成SelectAll()/ClearSelection()
                butSub0003_08.ImageIndex = 7;
                butSub0003_08.Text = Language.m_StrbutSub0003_08_02;
            }
            else
            {
                dgvSub0003_01.ClearSelection();//把所有列表元件的全選/取消全選改成SelectAll()/ClearSelection()
                butSub0003_08.ImageIndex = 8;
                butSub0003_08.Text = Language.m_StrbutSub0003_08_01;
            }
            m_blnApbSelectAll = (!m_blnApbSelectAll);
            //---按照『V8 功能選單』一個一個改 - 門區APB群組 ~ 全選/取消全選 整合成同一個按鈕
        }

        private void butSub0003_01_Click(object sender, EventArgs e)//編輯A.P.B
        {
            if (m_intdgvSub0003_01_id < 0)
            {
                return;
            }
            m_TPOld = m_tabMain.SelectedTab;//--2017/02/22 製作返回按鈕功能

            TabPage_Push();//m_StackTPOld.Push(m_TPOld);//--2017/02/22 製作返回按鈕功能
            m_tabSub000301.Parent = m_tabMain;
            m_tabMain.SelectedTab = m_tabSub000301;
            initSub000301UI();
 
            //--
            //同步子頁的選擇列表 at 2017/07/18
            for (int i = 0; i < dgvSub000301_01.Rows.Count; i++)
            {
                int id = Convert.ToInt32(dgvSub000301_01.Rows[i].Cells[1].Value.ToString());
                if (id != m_intdgvSub0003_01_id)
                {
                    dgvSub000301_01.Rows[i].Selected = false;
                }
                else
                {
                    dgvSub000301_01.Rows[i].Selected = true;
                }
            }
            //--

            DB2LeftSub000301UI(m_intdgvSub0003_01_id);

            txtSub000301_01.Focus();//--2017/03/30 頁面切換後，指定該頁面特定元件取的焦點(Focus)

            m_Sub000301ALInit.Clear();//add at 2017/10/06
            m_Sub000301ALInit.Add(txtSub000301_01.Text);//add at 2017/10/06
            m_Sub000301ALInit.Add(rdbSub000301_01.Checked.ToString());//add at 2017/10/06
            m_Sub000301ALInit.Add(rdbSub000301_02.Checked.ToString());//add at 2017/10/06
            m_Sub000301ALInit.Add(steSub000301_01.blnEnable.ToString());//add at 2017/10/06
            m_Sub000301ALInit.Add(steSub000301_01.StrValue);//add at 2017/10/06
            m_Sub000301ALInit.Add(steSub000301_01.ckb_001.Checked.ToString());//add at 2017/10/06
            m_Sub000301ALInit.Add(steSub000301_02.blnEnable.ToString());//add at 2017/10/06
            m_Sub000301ALInit.Add(steSub000301_02.StrValue);//add at 2017/10/06
            m_Sub000301ALInit.Add(steSub000301_02.ckb_001.Checked.ToString());//add at 2017/10/06
            m_Sub000301ALInit.Add(steSub000301_03.blnEnable.ToString());//add at 2017/10/06
            m_Sub000301ALInit.Add(steSub000301_03.StrValue);//add at 2017/10/06
            m_Sub000301ALInit.Add(steSub000301_03.ckb_001.Checked.ToString());//add at 2017/10/06
            m_Sub000301ALInit.Add(steSub000301_04.blnEnable.ToString());//add at 2017/10/06
            m_Sub000301ALInit.Add(steSub000301_04.StrValue);//add at 2017/10/06
            m_Sub000301ALInit.Add(steSub000301_04.ckb_001.Checked.ToString());//add at 2017/10/06
            m_Sub000301ALInit.Add(steSub000301_05.blnEnable.ToString());//add at 2017/10/06
            m_Sub000301ALInit.Add(steSub000301_05.StrValue);
            m_Sub000301ALInit.Add(steSub000301_05.ckb_001.Checked.ToString());//add at 2017/10/06
            m_Sub000301ALInit.Add(steSub000301_06.blnEnable.ToString());//add at 2017/10/06
            m_Sub000301ALInit.Add(steSub000301_06.StrValue);
            m_Sub000301ALInit.Add(steSub000301_06.ckb_001.Checked.ToString());//add at 2017/10/06
            m_Sub000301ALInit.Add(steSub000301_07.blnEnable.ToString());//add at 2017/10/06
            m_Sub000301ALInit.Add(steSub000301_07.StrValue);
            m_Sub000301ALInit.Add(steSub000301_07.ckb_001.Checked.ToString());//add at 2017/10/06
            m_Sub000301ALInit.Add(steSub000301_08.blnEnable.ToString());//add at 2017/10/06
            m_Sub000301ALInit.Add(steSub000301_08.StrValue);//add at 2017/10/06
            m_Sub000301ALInit.Add(steSub000301_08.ckb_001.Checked.ToString());//add at 2017/10/06

            //m_TPOld.Parent = null;//隱藏目前分頁，但是要分開寫，不可抓到就直接執行，必須先指定目前新分頁，否則系統會產生錯亂-2017/03/02
        }

        private void butSub0003_02_Click(object sender, EventArgs e)//新增A.P.B
        {
            m_TPOld = m_tabMain.SelectedTab;//--2017/02/22 製作返回按鈕功能

            TabPage_Push();//m_StackTPOld.Push(m_TPOld);//--2017/02/22 製作返回按鈕功能
            m_tabSub000301.Parent = m_tabMain;

            m_tabMain.SelectedTab = m_tabSub000301;
            initSub000301UI();
            m_intDB2LeftSub000301_id = -10;
            initLeftSub000301UI();

            //---
            //新增所有群組時都預設填入名稱
            //txtSub000301_01.Focus();//2017/03/30 頁面切換後，指定該頁面特定元件取的焦點(Focus)
            txtSub000301_01.Text = "group_apb_" + DateTime.Now.ToString("yyyyMMddHHmmss");
            //---新增所有群組時都預設填入名稱

            m_Sub000301ALInit.Clear();//add at 2017/10/06
            m_Sub000301ALInit.Add(txtSub000301_01.Text);//add at 2017/10/06
            m_Sub000301ALInit.Add(rdbSub000301_01.Checked.ToString());//add at 2017/10/06
            m_Sub000301ALInit.Add(rdbSub000301_02.Checked.ToString());//add at 2017/10/06
            m_Sub000301ALInit.Add(steSub000301_01.blnEnable.ToString());//add at 2017/10/06
            m_Sub000301ALInit.Add(steSub000301_01.StrValue);//add at 2017/10/06
            m_Sub000301ALInit.Add(steSub000301_01.ckb_001.Checked.ToString());//add at 2017/10/06
            m_Sub000301ALInit.Add(steSub000301_02.blnEnable.ToString());//add at 2017/10/06
            m_Sub000301ALInit.Add(steSub000301_02.StrValue);//add at 2017/10/06
            m_Sub000301ALInit.Add(steSub000301_02.ckb_001.Checked.ToString());//add at 2017/10/06
            m_Sub000301ALInit.Add(steSub000301_03.blnEnable.ToString());//add at 2017/10/06
            m_Sub000301ALInit.Add(steSub000301_03.StrValue);//add at 2017/10/06
            m_Sub000301ALInit.Add(steSub000301_03.ckb_001.Checked.ToString());//add at 2017/10/06
            m_Sub000301ALInit.Add(steSub000301_04.blnEnable.ToString());//add at 2017/10/06
            m_Sub000301ALInit.Add(steSub000301_04.StrValue);//add at 2017/10/06
            m_Sub000301ALInit.Add(steSub000301_04.ckb_001.Checked.ToString());//add at 2017/10/06
            m_Sub000301ALInit.Add(steSub000301_05.blnEnable.ToString());//add at 2017/10/06
            m_Sub000301ALInit.Add(steSub000301_05.StrValue);
            m_Sub000301ALInit.Add(steSub000301_05.ckb_001.Checked.ToString());//add at 2017/10/06
            m_Sub000301ALInit.Add(steSub000301_06.blnEnable.ToString());//add at 2017/10/06
            m_Sub000301ALInit.Add(steSub000301_06.StrValue);
            m_Sub000301ALInit.Add(steSub000301_06.ckb_001.Checked.ToString());//add at 2017/10/06
            m_Sub000301ALInit.Add(steSub000301_07.blnEnable.ToString());//add at 2017/10/06
            m_Sub000301ALInit.Add(steSub000301_07.StrValue);
            m_Sub000301ALInit.Add(steSub000301_07.ckb_001.Checked.ToString());//add at 2017/10/06
            m_Sub000301ALInit.Add(steSub000301_08.blnEnable.ToString());//add at 2017/10/06
            m_Sub000301ALInit.Add(steSub000301_08.StrValue);//add at 2017/10/06
            m_Sub000301ALInit.Add(steSub000301_08.ckb_001.Checked.ToString());//add at 2017/10/06

            //m_TPOld.Parent = null;//隱藏目前分頁，但是要分開寫，不可抓到就直接執行，必須先指定目前新分頁，否則系統會產生錯亂-2017/03/02
        }
        //Sub0003_end
        //Sub0001_start

        //---
        //製作多選支援左右鍵切換查詢+修改門區內容 ~ 撰寫儲存查詢授權紀錄變數相關
        public ArrayList m_ALDoorObj = new ArrayList();
        public int m_intDoorIndex;
        private void initSelectDoorArray(int select)
        {
	        m_ALDoorObj.Clear();
	        m_intDoorIndex = 0;
	        switch(select)
	        {
		        case 1:	
			        for (int i = 0; i < tvmSub0001_01.m_coll.Count; i++)
			        {
				        Tree_Node buf = (Tree_Node)(tvmSub0001_01.m_coll[i]);
                        if (buf.m_data != "")
                        {
                            if ((((buf.m_unit == -1) && Int32.Parse(buf.m_data) > 99)) || ((buf.m_unit != -1) && (Int32.Parse(buf.m_data) < 99)))
                            {
                                m_ALDoorObj.Add(buf);
                            }
                        }
			        }						
			        break;
		        case 2:
                    foreach (ListViewItem item in ltvSub0001_01.SelectedItems)
                    {
	                    Tree_Node buf_old = (Tree_Node)m_ALltvSub0001_01[item.Index];
                        Tree_Node buf = new Tree_Node(buf_old.m_id, buf_old.Text, buf_old.m_unit, buf_old.m_tree_level, buf_old.m_data);//修正門區列表重複選擇電梯樓層不能進去編輯的BUG
                        //---
                        //製作多選支援左右鍵切換查詢+修改門區內容 ~ m_tabSub0001右側按鈕遇到電梯選項無法正常工作修正
                        if (Int32.Parse(buf.m_data) > 99)
                        {
                            String Strid = "-1", Strunit = "-1", Strname = "", Strdoor_number = "-1";
                            String SQL = String.Format("SELECT ce.door_number AS door_number,c.name AS name,d.controller_id AS controller_id FROM door AS d,controller_extend AS ce,controller AS c WHERE (d.controller_id=ce.controller_sn) AND (d.controller_id=c.sn) AND (d.id={0});", buf.m_id);
                            MySqlDataReader Readerd_id = MySQL.GetDataReader(SQL);
                            while (Readerd_id.Read())
                            {
                                Strid = Readerd_id["controller_id"].ToString();
                                Strunit = "-1";
                                Strname = Readerd_id["name"].ToString();
                                Strdoor_number = Readerd_id["door_number"].ToString();
                                break;
                            }
                            Readerd_id.Close();
                            buf.m_id = Int32.Parse(Strid);
                            buf.m_unit = -1;
                            buf.Text = Strname;
                            buf.m_data = Strdoor_number;
                        }
                        //---製作多選支援左右鍵切換查詢+修改門區內容 ~ m_tabSub0001右側按鈕遇到電梯選項無法正常工作修正

                        m_ALDoorObj.Add(buf);
                    }
			        break;
                case 3:
                    for (int i = 0; i < tvmSub000200_01.m_coll.Count; i++)
			        {
                        Tree_Node buf1 = (Tree_Node)(tvmSub000200_01.m_coll[i]);
                        Tree_Node buf = new Tree_Node(buf1.m_id, buf1.Text, buf1.m_unit, buf1.m_tree_level, buf1.m_data);
                        if (buf.m_tree_level == 2)
                        {
                            String Strid = "-1", Strunit = "-1", Strname = "", Strdoor_number = "-1";
                            String SQL = String.Format("SELECT ce.door_number AS door_number,c.name AS name,d.controller_id AS controller_id FROM door AS d,controller_extend AS ce,controller AS c WHERE (d.controller_id=ce.controller_sn) AND (d.controller_id=c.sn) AND (d.id={0});", buf.m_id);
                            MySqlDataReader Readerd_id = MySQL.GetDataReader(SQL);
                            while (Readerd_id.Read())
                            {
                                Strid = Readerd_id["controller_id"].ToString();
                                Strunit = "-1";
                                Strname = Readerd_id["name"].ToString();
                                Strdoor_number = Readerd_id["door_number"].ToString();
                                break;
                            }
                            Readerd_id.Close();

                            buf.m_data = Strdoor_number;
                            if (Int32.Parse(Strdoor_number) > 99)
                            {
                                buf.m_id = Int32.Parse(Strid);
                                buf.m_unit = -1;
                                buf.Text = Strname;
                            }

                            m_ALDoorObj.Add(buf);
                        }
			        }
                    break;
	        }

            //---
            //修正多選電梯樓層時在顯示編輯UI會出現多個電梯控制器的選項
            ArrayList ALDoorbuf = new ArrayList();
            for (int i = 0; i < m_ALDoorObj.Count; i++)
            {
                bool blnEqual = false;
                Tree_Node bufA = (Tree_Node)m_ALDoorObj[i];
                Tree_Node bufB = null;
                for (int j = 0; j < ALDoorbuf.Count; j++)
                {
                    bufB = (Tree_Node)ALDoorbuf[j];
                    if (bufA.Text == bufB.Text)
                    {
                        blnEqual = true;
                        break;
                    }
                }
                if (blnEqual == false)
                {
                    ALDoorbuf.Add(bufA);
                }
                blnEqual = false;
            }
            m_ALDoorObj = ALDoorbuf;
            //---修正多選電梯樓層時在顯示編輯UI會出現多個電梯控制器的選項
        }
        //---製作多選支援左右鍵切換查詢+修改門區內容 ~ 撰寫儲存查詢授權紀錄變數相關

        private void butSub0001_04_Click(object sender, EventArgs e)//新增門區到區域內
        {
            if ((m_ControllerTree_NodeFun != null) && (tvmSub0001_02.SelectedNode != null))
            {
                String SQL = "";
                m_ControllerTree_NodeFun.getData(tvmSub0001_01);//抓取目前 被選取門區
                int area_id = ((Tree_Node)tvmSub0001_02.SelectedNode).m_id;
                String door_id = "";
                for (int i = 0; i < m_ControllerTree_NodeFun.m_ALget.Count; i++)
                {
                    door_id = m_ControllerTree_NodeFun.m_ALget[i].ToString();

                    //--
                    //防呆用，怕使用者未設定門區的細部資料，導致APB 無法顯示門區 2017/08/02
                    bool blndoor_extend = false;
                    SQL = String.Format("SELECT door_id FROM door_extend WHERE door_id={0};", door_id);
                    MySqlDataReader Reader_Date = MySQL.GetDataReader(SQL);
                    while (Reader_Date.Read())
                    {
                        blndoor_extend = true;
                        break;
                    }
                    Reader_Date.Close();
                    if (!blndoor_extend)
                    {
                        SQL = String.Format(@"INSERT INTO door_extend (door_id,base,pass,open,anti_de,detect,button,anti_co,overtime,violent,pass_mode,auto_mode,access_den,state) VALUES ({0},'{1}','{2}','{3}','{4}','{5}','{6}','{7}','{8}','{9}','{10}','{11}','{12}',1);",
                                            door_id,
                                            ",0,0",
                                            "0,0,0,0,0-0-0,0-0",
                                            "0,0,0,0",
                                            "0,0,0,0",
                                            "0,0,0,0",
                                            "0,0,0,0,0",
                                            ",0,0,0,0,1",
                                            "0,1,0,0,0,1,1",
                                            "0,0,0,1,1",
                                            "0-0,0-0,0-0,0-0,0,0,0,00:00~00:00,00:00~00:00,00:00~00:00,00:00~00:00,00:00~00:00,00:00~00:00,00:00~00:00,00:00~00:00",
                                            "0,0,0,00:00~00:00,00:00~00:00,00:00~00:00,00:00~00:00,00:00~00:00,00:00~00:00,00:00~00:00,00:00~00:00",
                                            "0,0,0,1,1");//修正BUG-無法顯示門區設定(增加access_den值)
                        MySQL.InsertUpdateDelete(SQL);
                    }
                    //--

                    SQL = String.Format("INSERT INTO area_detail (area_id,door_id,state) SELECT {0},{1},1 FROM DUAL WHERE NOT EXISTS(SELECT area_id FROM area_detail WHERE area_id = {0} AND door_id = {1});", area_id, door_id);
                    MySQL.InsertUpdateDelete(SQL);
                }
                initltvSub0001_01();//顯示對應區域下的門區資料
            }

        }
        
        private void butSub0001_14_Click(object sender, EventArgs e)//離開
        {
            if (m_blnrunbutSub000200_09)
            {
                m_blnrunbutSub000200_09 = false;
                initvmSub000200_01();
                DB2LeftSub000200UI(m_intDB2LeftSub000200_id);
            }
            Leave_function();
        }
        
        private void butSub000100_09_Click(object sender, EventArgs e)//門區設定的離開
        {
            m_Sub000100ALData.Clear();//add at 2017/10/06
            Sub000100_getUIValue();//add at 2017/10/06
            m_Sub000100ALData.Add(m_StrBase);//add at 2017/10/06
            m_Sub000100ALData.Add(m_StrPass);//add at 2017/10/06
            m_Sub000100ALData.Add(m_StrOpen);//add at 2017/10/06
            m_Sub000100ALData.Add(m_StrAnti_de);//add at 2017/10/06
            m_Sub000100ALData.Add(m_StrDetect);//add at 2017/10/06
            m_Sub000100ALData.Add(m_StrButton);//add at 2017/10/06
            m_Sub000100ALData.Add(m_StrAnti_co);//add at 2017/10/06
            m_Sub000100ALData.Add(m_StrOvertime);//add at 2017/10/06
            m_Sub000100ALData.Add(m_StrViolent);//add at 2017/10/06
            m_Sub000100ALData.Add(m_StrPass_mode);//add at 2017/10/06
            m_Sub000100ALData.Add(m_StrAuto_mode);//add at 2017/10/06
            m_Sub000100ALData.Add(m_StrAccess_den);//撰寫door_extend表中access_den欄位的相關程式碼

            if ((m_StrSub000100door_id == "-1") || (!gpbSub000100_01.Enabled) || (CheckUIVarNotChange(m_Sub000100ALInit, m_Sub000100ALData)))//if ((m_StrSub000100door_id == "-1") || (!gpbSub000100_01.Enabled))
            {
                Leave_function();
            }
            else
            {
                DialogResult myResult = MessageBox.Show(Language.m_StrControllerMsg00, butSub000100_09.Text, MessageBoxButtons.YesNo, MessageBoxIcon.Question);
                if (myResult == DialogResult.Yes)
                {
                    Leave_function();
                }
            }
        }
        
        private void butSub000100_07_Click(object sender, EventArgs e)//填入預設值
        {
            String SQL = "";
            int door_number = 0;
            int number = 0;
            SQL = String.Format("SELECT d.controller_door_index AS d_num,c_e.door_number AS num FROM door AS d,controller_extend AS c_e WHERE (d.id={0}) AND (d.controller_id=c_e.controller_sn);", m_StrSub000100door_id);//SQL = String.Format("SELECT controller_door_index AS d_num FROM door WHERE id={0};", m_StrSub000100door_id);
            MySqlDataReader Readerd_num = MySQL.GetDataReader(SQL);
            while (Readerd_num.Read())
            {
                door_number = Convert.ToInt32(Readerd_num["d_num"].ToString());
                number = Convert.ToInt32(Readerd_num["num"].ToString());
                break;
            }
            Readerd_num.Close();

            Sub000100_initUIVar(door_number, number, true);//執行UI變數回復預設值
            
            Sub000100_setUIValue();//

            MessageBox.Show(Language.m_StrbutSub000100_07Msg00, butSub000100_07.Text, MessageBoxButtons.OK, MessageBoxIcon.Information);
        }

        //--
        //add 2017/10/23
        private void SwitchDoorElevatorUI(bool blnDoor = true)
        {
            if(blnDoor)
            {
                gpbSub000100_04.Visible = true;
                gpbSub000100_06.Visible = true;
                gpbSub000100_07.Visible = true;
                gpbSub000100_09.Visible = true;
                gpbSub000100_10.Visible = true;
                gpbSub000100_13.Visible = true;
                gpbSub000100_02.Location = new Point(489, 33);
                gpbSub000100_05.Location = new Point(489, 284);//->(6, 678)
                gpbSub000100_08.Location = new Point(489, 438);//->(6, 468)
                gpbSub000100_11.Location = new Point(976, 33);//->(491, 36)
                gpbSub000100_12.Location = new Point(976, 550);//->(491, 553)
            }
            else
            {
                gpbSub000100_04.Visible = false;
                gpbSub000100_06.Visible = false;
                gpbSub000100_07.Visible = false;
                gpbSub000100_09.Visible = false;
                gpbSub000100_10.Visible = false;
                gpbSub000100_13.Visible = false;
                gpbSub000100_02.Location = new Point(6, 205);
                gpbSub000100_05.Location = new Point(6, 205 + 251);//->(6, 601)
                gpbSub000100_08.Location = new Point(6, 205 + 251+154);
                gpbSub000100_11.Location = new Point(491, 31);//->(976, 38)
                gpbSub000100_12.Location = new Point(491, 548);//->(976, 555)
            }
        }
        //--

        //--
        //add 2017/10/27
        public void ShowtabSub000100UI(int id, int unit, int doornumber, String name)//自己,父親,總門數
        {
            if (((unit == -1) && (doornumber > 99)) || ((unit != -1) && (doornumber < 99)))
            {//電梯控制器 或 一般門
                m_TPOld = m_tabMain.SelectedTab;//--2017/02/22 製作返回按鈕功能
                TabPage_Push();//m_StackTPOld.Push(m_TPOld);//--2017/02/22 製作返回按鈕功能

		        //---
                //按照『V8 功能選單』一個一個改 - 設置門區/電梯 ~ 要可修改門區名稱[只有門才能修改]
                if (unit != -1)
                {
                    MySqlDataReader Readerd_name = MySQL.GetDataReader(String.Format("SELECT name FROM door WHERE id={0} LIMIT 0,1;",id));
                    while (Readerd_name.Read())
                    {
                        name = Readerd_name[0].ToString();
                        break;
                    }
                    Readerd_name.Close();
                }
                //---按照『V8 功能選單』一個一個改 - 設置門區/電梯 ~要可修改門區名稱[只有門才能修改]

                initSub000100UI(name);//為了讓門區抬頭加上門區名稱 at 2017/09/19

                String SQL = "";
                if (unit == -1)//電梯控制器
                {
                    SQL = String.Format("SELECT id FROM door WHERE (controller_door_index=1) AND (controller_id={0});", id);//找出該電梯控制器的第一個樓層的id
                    MySqlDataReader Readerd_id = MySQL.GetDataReader(SQL);
                    while (Readerd_id.Read())
                    {
                        m_StrSub000100door_id = Readerd_id["id"].ToString();
                        break;
                    }
                    Readerd_id.Close();
                    txtSub000100_01.ReadOnly = true;//按照『V8 功能選單』一個一個改 - 設置門區/電梯 ~ 要可修改門區名稱[只有門才能修改]
                }
                else//一般門
                {
                    m_StrSub000100door_id = id + "";
                    txtSub000100_01.ReadOnly = false;//按照『V8 功能選單』一個一個改 - 設置門區/電梯 ~ 要可修改門區名稱[只有門才能修改]
                }

                //---
                //SYCG模式下-建立/暫存 當下要操作的SYDM ID
                SQL = String.Format("SELECT d.id AS id,c.sydm_id AS sydm_id FROM door AS d,controller AS c WHERE (d.controller_id=c.sn) AND (d.id={0});", m_StrSub000100door_id);
                MySqlDataReader Readerd_SYDMid = MySQL.GetDataReader(SQL);
                while (Readerd_SYDMid.Read())
                {
                    m_intSYDM_id = Convert.ToInt32(Readerd_SYDMid["sydm_id"].ToString());//SYCG模式下-建立/暫存 當下要操作的SYDM ID
                    break;
                }
                Readerd_SYDMid.Close();
                //---SYCG模式下-建立/暫存 當下要操作的SYDM ID
                
                int door_number = 0;
                int number = 0;
                bool blnDoor = true;//add 2017/10/23
                SQL = String.Format("SELECT d.controller_door_index AS d_num,c_e.door_number AS num FROM door AS d,controller_extend AS c_e WHERE (d.id={0}) AND (d.controller_id=c_e.controller_sn);", m_StrSub000100door_id);//SQL = String.Format("SELECT controller_door_index AS d_num FROM door WHERE id={0};", Strdoorid);
                MySqlDataReader Readerd_num = MySQL.GetDataReader(SQL);
                while (Readerd_num.Read())
                {
                    door_number = Convert.ToInt32(Readerd_num["d_num"].ToString());
                    number = Convert.ToInt32(Readerd_num["num"].ToString());

                    //--
                    //add 2017/10/23
                    if (number > 99)
                    {
                        blnDoor = false;
                        labSub000100.Text = Language.m_StrTabPageTag000101 + "-" + name;//新增引數為了顯示門區名 at 2017/09/19
                        m_tabSub000100.Text = Language.m_StrTabPageTag000101;//修改頁籤顯示文字
                        //--txtSub000100_01.Visible = false;//電梯模式下不用顯示門名，因為電梯實際上只有一門
                    }
                    else
                    {
                        blnDoor = true;
                        labSub000100.Text = Language.m_StrTabPageTag000100 + "-" + name;//新增引數為了顯示門區名 at 2017/09/19
                        m_tabSub000100.Text = Language.m_StrTabPageTag000100;//修改頁籤顯示文字
                        //--txtSub000100_01.Visible = true;//非電梯模式下要顯示門名
                        
                    }
                    txtSub000100_01.Text = name;
                    //--

                    break;
                }
                Readerd_num.Close();
                Sub000100_initUIVar(door_number, number, true);//把UI變數初始化

                m_blnSub000100modified = false;//修正 無法紀錄 door_extend 的BUG at 2017/09/19
                SQL = String.Format("SELECT base,pass,open,anti_de,detect,button,anti_co,overtime,violent,pass_mode,auto_mode,access_den FROM door_extend WHERE door_id={0};", m_StrSub000100door_id);
                MySqlDataReader DataReader = MySQL.GetDataReader(SQL);
                while (DataReader.Read())
                {
                    m_blnSub000100modified = true;
                    m_StrBase = DataReader["base"].ToString();// text 'xxxxx,0,0'
                    m_StrPass = DataReader["pass"].ToString();// text '0,0,0,0,0-0-0,1-0-0'
                    m_StrOpen = DataReader["open"].ToString();// text '0,0,0,0'
                    m_StrAnti_de = DataReader["anti_de"].ToString();// text '0,0,0,0'
                    m_StrDetect = DataReader["detect"].ToString();// text '0,0,0,0'
                    m_StrButton = DataReader["button"].ToString();// text '0,0,0,0,0'
                    m_StrAnti_co = DataReader["anti_co"].ToString();// text 'xxxxx,0,0,0,0,0'
                    m_StrOvertime = DataReader["overtime"].ToString();// text '0,0,0,0,0,0,0'
                    m_StrViolent = DataReader["violent"].ToString();// text '0,0,0,0,0'
                    m_StrPass_mode = DataReader["pass_mode"].ToString();// text '0-0,0-0,0-0,0-0,0,0,0,0:0~0:0,0:0~0:0,0:0~0:0,0:0~0:0,0:0~0:0,0:0~0:0,0:0~0:0,0:0~0:0'
                    m_StrAuto_mode = DataReader["auto_mode"].ToString();// text '0,0,0,0:0~0:0,0:0~0:0,0:0~0:0,0:0~0:0,0:0~0:0,0:0~0:0,0:0~0:0,0:0~0:0'
                    m_StrAccess_den = DataReader["access_den"].ToString();// text '0,0,0,0,0' //撰寫door_extend表中access_den欄位的相關程式碼
                    break;
                }
                DataReader.Close();

                Sub000100_setUIValue();

                if (true)
                {
                    m_tabSub000100.Parent = m_tabMain;//門區設定UI顯示 at 2017/07/03
                    m_tabMain.SelectedTab = m_tabSub000100;//門區設定UI顯示 at 2017/07/03

                    m_Sub000100ALInit.Clear();//add at 2017/10/06
                    m_Sub000100ALInit.Add(m_StrBase);//add at 2017/10/06
                    m_Sub000100ALInit.Add(m_StrPass);//add at 2017/10/06
                    m_Sub000100ALInit.Add(m_StrOpen);//add at 2017/10/06
                    m_Sub000100ALInit.Add(m_StrAnti_de);//add at 2017/10/06
                    m_Sub000100ALInit.Add(m_StrDetect);//add at 2017/10/06
                    m_Sub000100ALInit.Add(m_StrButton);//add at 2017/10/06
                    m_Sub000100ALInit.Add(m_StrAnti_co);//add at 2017/10/06
                    m_Sub000100ALInit.Add(m_StrOvertime);//add at 2017/10/06
                    m_Sub000100ALInit.Add(m_StrViolent);//add at 2017/10/06
                    m_Sub000100ALInit.Add(m_StrPass_mode);//add at 2017/10/06
                    m_Sub000100ALInit.Add(m_StrAuto_mode);//add at 2017/10/06
                    m_Sub000100ALInit.Add(m_StrAccess_den);//撰寫door_extend表中access_den欄位的相關程式碼

                    //--
                    //add 2017/10/23
                    #if !(Delta_Tool)
                        SwitchDoorElevatorUI(blnDoor);
                    #endif
                    //--
                }
                else
                {
                    m_tabSub000101.Parent = m_tabMain;//電梯設定UI顯示 at 2017/07/03
                    m_tabMain.SelectedTab = m_tabSub000101;//電梯設定UI顯示 at 2017/07/03
                }
            }

            txtSub000100_01.Focus();//製作多選支援左右鍵切換查詢+修改門區內容 ~ 確保多選時左右鍵可以正常工作
        }
        //--

        private void butSub0001_05_Click(object sender, EventArgs e)//開啟門區編輯
        {
            //---
            //製作多選支援左右鍵切換查詢+修改門區內容 ~ 撰寫儲存查詢授權紀錄變數相關
            
            //Tree_Node tmp = ((Tree_Node)tvmSub0001_01.SelectedNode);//抓取目前被選取節點到暫存變數中 at 2017/07/06
            //tvmSub0001_01.SelectedNode = null;//清空被選取節點紀錄，防止下次沒選擇有殘值，造成誤動作 at 2017/07/06
            
            initSelectDoorArray(1);
            Tree_Node tmp = null;
            if(m_ALDoorObj.Count>1)
            {
                tmp = (Tree_Node)m_ALDoorObj[0];
            }
            else
            {
                tmp = ((Tree_Node)tvmSub0001_01.SelectedNode);//抓取目前被選取節點到暫存變數中 at 2017/07/06
                tvmSub0001_01.SelectedNode = null;//清空被選取節點紀錄，防止下次沒選擇有殘值，造成誤動作 at 2017/07/06
            }
            //---製作多選支援左右鍵切換查詢+修改門區內容 ~ 撰寫儲存查詢授權紀錄變數相關

            //--
            //modified 2017/10/27
            if (tmp != null)
            {
                try
                {
                    ShowtabSub000100UI(tmp.m_id, tmp.m_unit, Int32.Parse(tmp.m_data), tmp.Text);
                }
                catch
                {
                }
            }
            /*
            if ((tmp != null) && (tmp.m_unit != -1))//判斷是否為門區節點 at 2017/07/06
            {
                m_TPOld = m_tabMain.SelectedTab;//--2017/02/22 製作返回按鈕功能

                TabPage_Push();//m_StackTPOld.Push(m_TPOld);//--2017/02/22 製作返回按鈕功能

                initSub000100UI(tmp.Text);//為了讓門區抬頭加上門區名稱 at 2017/09/19
                //txtSub000100_01.Text = tmp.Text;//抓取門區名稱
                String Strdoorid = tmp.m_id.ToString();//抓取門區在資料庫的索引值
                m_StrSub000100door_id = Strdoorid;

                String SQL = "";
                int door_number = 0;
                int number = 0;
                bool blnDoor = true;//add 2017/10/23
                SQL = String.Format("SELECT d.controller_door_index AS d_num,c_e.door_number AS num FROM door AS d,controller_extend AS c_e WHERE (d.id={0}) AND (d.controller_id=c_e.controller_sn);", Strdoorid);//SQL = String.Format("SELECT controller_door_index AS d_num FROM door WHERE id={0};", Strdoorid);
                MySqlDataReader Readerd_num = MySQL.GetDataReader(SQL);
                while (Readerd_num.Read())
                {
                    door_number = Convert.ToInt32(Readerd_num["d_num"].ToString());
                    number = Convert.ToInt32(Readerd_num["num"].ToString());

                    //--
                    //add 2017/10/23
                    if (number > 99)
                    {
                        blnDoor = false;
                        labSub000100.Text = Language.m_StrTabPageTag000101 + "-" + tmp.Text;//新增引數為了顯示門區名 at 2017/09/19
                        m_tabSub000100.Text = Language.m_StrTabPageTag000101;
                    }
                    else
                    {
                        blnDoor = true;
                        labSub000100.Text = Language.m_StrTabPageTag000100 + "-" + tmp.Text;//新增引數為了顯示門區名 at 2017/09/19
                        m_tabSub000100.Text = Language.m_StrTabPageTag000100;
                    }
                    txtSub000100_01.Text = tmp.Text;//抓取門區名稱
                    //--

                    break;
                }
                Readerd_num.Close();
                Sub000100_initUIVar(door_number, number, true);//把UI變數初始化

                m_blnSub000100modified = false;//修正 無法紀錄 door_extend 的BUG at 2017/09/19
                SQL = String.Format("SELECT base,pass,open,anti_de,detect,button,anti_co,overtime,violent,pass_mode,auto_mode FROM door_extend WHERE door_id={0};", Strdoorid);
                MySqlDataReader DataReader = MySQL.GetDataReader(SQL);
                while (DataReader.Read())
                {
                    m_blnSub000100modified = true;
                    m_StrBase = DataReader["base"].ToString();// text 'xxxxx,0,0'
                    m_StrPass = DataReader["pass"].ToString();// text '0,0,0,0,0-0-0,1-0-0'
                    m_StrOpen = DataReader["open"].ToString();// text '0,0,0,0'
                    m_StrAnti_de = DataReader["anti_de"].ToString();// text '0,0,0,0'
                    m_StrDetect = DataReader["detect"].ToString();// text '0,0,0,0'
                    m_StrButton = DataReader["button"].ToString();// text '0,0,0,0,0'
                    m_StrAnti_co = DataReader["anti_co"].ToString();// text 'xxxxx,0,0,0,0,0'
                    m_StrOvertime = DataReader["overtime"].ToString();// text '0,0,0,0,0,0,0'
                    m_StrViolent = DataReader["violent"].ToString();// text '0,0,0,0,0'
                    m_StrPass_mode = DataReader["pass_mode"].ToString();// text '0-0,0-0,0-0,0-0,0,0,0,0:0~0:0,0:0~0:0,0:0~0:0,0:0~0:0,0:0~0:0,0:0~0:0,0:0~0:0,0:0~0:0'
                    m_StrAuto_mode = DataReader["auto_mode"].ToString();// text '0,0,0,0:0~0:0,0:0~0:0,0:0~0:0,0:0~0:0,0:0~0:0,0:0~0:0,0:0~0:0,0:0~0:0'
                    break;
                }
                DataReader.Close();

                Sub000100_setUIValue();

                if (true)
                {
                    m_tabSub000100.Parent = m_tabMain;//門區設定UI顯示 at 2017/07/03
                    m_tabMain.SelectedTab = m_tabSub000100;//門區設定UI顯示 at 2017/07/03

                    m_Sub000100ALInit.Clear();//add at 2017/10/06
                    m_Sub000100ALInit.Add(m_StrBase);//add at 2017/10/06
                    m_Sub000100ALInit.Add(m_StrPass);//add at 2017/10/06
                    m_Sub000100ALInit.Add(m_StrOpen);//add at 2017/10/06
                    m_Sub000100ALInit.Add(m_StrAnti_de);//add at 2017/10/06
                    m_Sub000100ALInit.Add(m_StrDetect);//add at 2017/10/06
                    m_Sub000100ALInit.Add(m_StrButton);//add at 2017/10/06
                    m_Sub000100ALInit.Add(m_StrAnti_co);//add at 2017/10/06
                    m_Sub000100ALInit.Add(m_StrOvertime);//add at 2017/10/06
                    m_Sub000100ALInit.Add(m_StrViolent);//add at 2017/10/06
                    m_Sub000100ALInit.Add(m_StrPass_mode);//add at 2017/10/06
                    m_Sub000100ALInit.Add(m_StrAuto_mode);//add at 2017/10/06

                    //--
                    //add 2017/10/23
                    #if !(Delta_Tool)
                        SwitchDoorElevatorUI(blnDoor);
                    #endif
                    //--
                }
                else
                {
                    m_tabSub000101.Parent = m_tabMain;//電梯設定UI顯示 at 2017/07/03
                    m_tabMain.SelectedTab = m_tabSub000101;//電梯設定UI顯示 at 2017/07/03
                }
            }
            */
            //--
            //--txtSub000101_01.Focus();//--2017/03/30 頁面切換後，指定該頁面特定元件取的焦點(Focus)
        }

        private void butSub0001_12_Click(object sender, EventArgs e)
        {
            int index=-1;
            //---
            //製作多選支援左右鍵切換查詢+修改門區內容 ~ 撰寫儲存查詢授權紀錄變數相關
            
            /*
            foreach (ListViewItem item in ltvSub0001_01.SelectedItems)
            {
                index=item.Index;
            }
            */

            initSelectDoorArray(2);
            if (m_ALDoorObj.Count > 0)
            {
                index = 0;
            }
            //---製作多選支援左右鍵切換查詢+修改門區內容 ~ 撰寫儲存查詢授權紀錄變數相關
            if (index != -1)
            {
                Tree_Node tmp = (Tree_Node)m_ALDoorObj[0];//製作多選支援左右鍵切換查詢+修改門區內容 ~ 撰寫儲存查詢授權紀錄變數相關 Tree_Node tmp = (Tree_Node)m_ALltvSub0001_01[index];
                //--
                //modified 2017/10/27
                if (tmp != null)
                {
                    //---
                    //修正門區列表只選電梯樓層不能進去編輯的BUG
                    /*
                    if (Int32.Parse(tmp.m_data) < 99)
                    {
                        ShowtabSub000100UI(tmp.m_id, tmp.m_unit, Int32.Parse(tmp.m_data), tmp.Text);
                    }
                    else
                    {
                        String Strid="-1", Strunit="-1", Strname="", Strdoor_number="-1";
                        String SQL = String.Format("SELECT ce.door_number AS door_number,c.name AS name,d.controller_id AS controller_id FROM door AS d,controller_extend AS ce,controller AS c WHERE (d.controller_id=ce.controller_sn) AND (d.controller_id=c.sn) AND (d.id={0});", tmp.m_id);
                        MySqlDataReader Readerd_id = MySQL.GetDataReader(SQL);
                        while (Readerd_id.Read())
                        {
                            Strid = Readerd_id["controller_id"].ToString();
                            Strunit = "-1";
                            Strname = Readerd_id["name"].ToString();
                            Strdoor_number = Readerd_id["door_number"].ToString();
                            break;
                        }
                        Readerd_id.Close();
                        ShowtabSub000100UI(Int32.Parse(Strid), Int32.Parse(Strunit), Int32.Parse(Strdoor_number), Strname);
                    }
                    */
                    ShowtabSub000100UI(tmp.m_id, tmp.m_unit, Int32.Parse(tmp.m_data), tmp.Text);
                    //---修正門區列表只選電梯樓層不能進去編輯的BUG
                }
                /*
                if ((tmp != null) && (tmp.m_unit != -1))//判斷是否為門區節點 at 2017/07/06
                {
                    m_TPOld = m_tabMain.SelectedTab;//--2017/02/22 製作返回按鈕功能

                    TabPage_Push();//m_StackTPOld.Push(m_TPOld);//--2017/02/22 製作返回按鈕功能

                    initSub000100UI(tmp.Text);//為了讓門區抬頭加上門區名稱 at 2017/09/19
                    //txtSub000100_01.Text = tmp.Text;//抓取門區名稱
                    String Strdoorid = tmp.m_id.ToString();//抓取門區在資料庫的索引值
                    m_StrSub000100door_id = Strdoorid;

                    String SQL = "";
                    int door_number = 0;
                    int number = 0;
                    bool blnDoor = true;//add 2017/10/23
                    SQL = String.Format("SELECT d.controller_door_index AS d_num,c_e.door_number AS num FROM door AS d,controller_extend AS c_e WHERE (d.id={0}) AND (d.controller_id=c_e.controller_sn);", Strdoorid);//SQL = String.Format("SELECT controller_door_index AS d_num FROM door WHERE id={0};", Strdoorid);
                    MySqlDataReader Readerd_num = MySQL.GetDataReader(SQL);
                    while (Readerd_num.Read())
                    {
                        door_number = Convert.ToInt32(Readerd_num["d_num"].ToString());
                        number = Convert.ToInt32(Readerd_num["num"].ToString());

                        //--
                        //add 2017/10/23
                        if (number > 99)
                        {
                            blnDoor = false;
                            labSub000100.Text = Language.m_StrTabPageTag000101 + "-" + tmp.Text;//新增引數為了顯示門區名 at 2017/09/19
                            m_tabSub000100.Text = Language.m_StrTabPageTag000101;
                        }
                        else
                        {
                            blnDoor = true;
                            labSub000100.Text = Language.m_StrTabPageTag000100 + "-" + tmp.Text;//新增引數為了顯示門區名 at 2017/09/19
                            m_tabSub000100.Text = Language.m_StrTabPageTag000100;
                        }
                        txtSub000100_01.Text = tmp.Text;//抓取門區名稱
                        //--

                        break;
                    }
                    Readerd_num.Close();
                    Sub000100_initUIVar(door_number, number, true);//把UI變數初始化

                    m_blnSub000100modified = false;//修正 無法紀錄 door_extend 的BUG at 2017/09/19
                    SQL = String.Format("SELECT base,pass,open,anti_de,detect,button,anti_co,overtime,violent,pass_mode,auto_mode FROM door_extend WHERE door_id={0};", Strdoorid);
                    MySqlDataReader DataReader = MySQL.GetDataReader(SQL);
                    while (DataReader.Read())
                    {
                        m_blnSub000100modified = true;
                        m_StrBase = DataReader["base"].ToString();// text 'xxxxx,0,0'
                        m_StrPass = DataReader["pass"].ToString();// text '0,0,0,0,0-0-0,1-0-0'
                        m_StrOpen = DataReader["open"].ToString();// text '0,0,0,0'
                        m_StrAnti_de = DataReader["anti_de"].ToString();// text '0,0,0,0'
                        m_StrDetect = DataReader["detect"].ToString();// text '0,0,0,0'
                        m_StrButton = DataReader["button"].ToString();// text '0,0,0,0,0'
                        m_StrAnti_co = DataReader["anti_co"].ToString();// text 'xxxxx,0,0,0,0,0'
                        m_StrOvertime = DataReader["overtime"].ToString();// text '0,0,0,0,0,0,0'
                        m_StrViolent = DataReader["violent"].ToString();// text '0,0,0,0,0'
                        m_StrPass_mode = DataReader["pass_mode"].ToString();// text '0-0,0-0,0-0,0-0,0,0,0,0:0~0:0,0:0~0:0,0:0~0:0,0:0~0:0,0:0~0:0,0:0~0:0,0:0~0:0,0:0~0:0'
                        m_StrAuto_mode = DataReader["auto_mode"].ToString();// text '0,0,0,0:0~0:0,0:0~0:0,0:0~0:0,0:0~0:0,0:0~0:0,0:0~0:0,0:0~0:0,0:0~0:0'
                        break;
                    }
                    DataReader.Close();

                    Sub000100_setUIValue();

                    if (true)
                    {
                        m_tabSub000100.Parent = m_tabMain;//門區設定UI顯示 at 2017/07/03
                        m_tabMain.SelectedTab = m_tabSub000100;//門區設定UI顯示 at 2017/07/03

                        m_Sub000100ALInit.Clear();//add at 2017/10/06
                        m_Sub000100ALInit.Add(m_StrBase);//add at 2017/10/06
                        m_Sub000100ALInit.Add(m_StrPass);//add at 2017/10/06
                        m_Sub000100ALInit.Add(m_StrOpen);//add at 2017/10/06
                        m_Sub000100ALInit.Add(m_StrAnti_de);//add at 2017/10/06
                        m_Sub000100ALInit.Add(m_StrDetect);//add at 2017/10/06
                        m_Sub000100ALInit.Add(m_StrButton);//add at 2017/10/06
                        m_Sub000100ALInit.Add(m_StrAnti_co);//add at 2017/10/06
                        m_Sub000100ALInit.Add(m_StrOvertime);//add at 2017/10/06
                        m_Sub000100ALInit.Add(m_StrViolent);//add at 2017/10/06
                        m_Sub000100ALInit.Add(m_StrPass_mode);//add at 2017/10/06
                        m_Sub000100ALInit.Add(m_StrAuto_mode);//add at 2017/10/06

                        //--
                        //add 2017/10/23
                        #if !(Delta_Tool)
                            SwitchDoorElevatorUI(blnDoor);
                        #endif
                        //--
                    }
                    else
                    {
                        m_tabSub000101.Parent = m_tabMain;//電梯設定UI顯示 at 2017/07/03
                        m_tabMain.SelectedTab = m_tabSub000101;//電梯設定UI顯示 at 2017/07/03
                    }
                }
                */
            }
        }

        private void butSub0001_06_Click(object sender, EventArgs e)//從區域移除門區資料
        {
            String SQL = "";
            foreach (ListViewItem item in ltvSub0001_01.SelectedItems)
            {
                Tree_Node tmp_Node = (Tree_Node)m_ALltvSub0001_01[item.Index];
                SQL = String.Format("DELETE FROM area_detail WHERE area_id={0} AND door_id={1};", tmp_Node.m_unit, tmp_Node.m_id);
                MySQL.InsertUpdateDelete(SQL);
            }
            initltvSub0001_01();//顯示對應區域下的門區資料
        }

        private void butSub0001_07_Click(object sender, EventArgs e)//新增區域
        {
            Tree_Node tmp = null;//按照『V8 功能選單』一個一個改 - 門區配置 ~ 只能單層不允許多階層 Tree_Node tmp = (Tree_Node)tvmSub0001_02.SelectedNode;

            AddArea AddArea = new AddArea(tmp);
            AddArea.ShowDialog();

            tvmSub0001_02.SelectedNode = null;
            initvmSub0001_02();//更新UI
        }

        private void butSub0001_08_Click(object sender, EventArgs e)//刪除區域 //按照『V8 功能選單』一個一個改 - 門區 [完成] ~ 刪除非空區域要有提示詢問
        {
            bool blnHasData = true;
            bool blnDelete = true;
            MySqlDataReader ReaderdData = null;

            Tree_Node tmp = (Tree_Node)tvmSub0001_02.SelectedNode;
            if(tmp!=null)
            {
                String SQL = "";

                SQL = String.Format("SELECT id FROM area WHERE unit = {0};", tmp.m_id);//判斷是否有子區域
                ReaderdData = MySQL.GetDataReader(SQL);
                if (ReaderdData.HasRows)
                {
                    blnHasData = true;
                }
                else
                {
                    blnHasData = false;
                }
                ReaderdData.Close();
                if (blnHasData == false)
                {
                    SQL = String.Format("SELECT area_id FROM area_detail WHERE area_id = {0};", tmp.m_id);//判斷區域內有門或電梯
                    ReaderdData = null;
                    ReaderdData = MySQL.GetDataReader(SQL);
                    if (ReaderdData.HasRows)
                    {
                        blnHasData = true;
                    }
                    ReaderdData.Close();
                }

                if (blnHasData == true)
                {
                    DialogResult myResult = MessageBox.Show(Language.m_StrDeleteAreaMsg01, butSub0001_08.Text.Trim(), MessageBoxButtons.YesNo, MessageBoxIcon.Question);//區域內有資料，提示是否要繼續刪除
                    if (myResult == DialogResult.Yes)
                    {//按了是
                        blnDelete = true;
                    }
                    else if (myResult == DialogResult.No)
                    {//按了否
                        blnDelete = false;
                    }
                }

                if (blnDelete == true)
                {
                    SQL = String.Format("DELETE FROM area WHERE id = {0} OR unit = {0};", tmp.m_id);//OR 部份是為了刪除子目錄
                    MySQL.InsertUpdateDelete(SQL);
                    SQL = String.Format("DELETE FROM area_detail WHERE area_id = {0};", tmp.m_id);//刪除門區
                    MySQL.InsertUpdateDelete(SQL);

                    initvmSub0001_02();//更新UI
                    initltvSub0001_01();//更新UI
                }
            }
        }

        private void butSub0001_02_Click(object sender, EventArgs e)//全選
        {
            for (int i=0; i < tvmSub0001_01.Nodes.Count; i++)
            {
                Tree_Node Root_Node = (Tree_Node)tvmSub0001_01.Nodes[i];
                Root_Node.Checked=true;
                ControllerTree_NodeFun.SetChildNodeCheckedState(Root_Node, Root_Node.Checked);//設置子節點狀態
            }
        }

        private void butSub0001_03_Click(object sender, EventArgs e)//取消全選
        {
            for (int i = 0; i < tvmSub0001_01.Nodes.Count; i++)
            {
                Tree_Node Root_Node = (Tree_Node)tvmSub0001_01.Nodes[i];
                Root_Node.Checked = false;
                ControllerTree_NodeFun.SetChildNodeCheckedState(Root_Node, Root_Node.Checked);//設置子節點狀態
            }
        }

        private void tvmSub0001_01_AfterSelect(object sender, TreeViewEventArgs e)//抓取被選擇的節點，並判斷是否為門區
        {
            Tree_Node tmp = ((Tree_Node)tvmSub0001_01.SelectedNode);
            if (tmp.m_unit != -1)
            {
                //MessageBox.Show(tmp.Text);
            }
        }

        private void tvmSub0001_02_AfterSelect(object sender, TreeViewEventArgs e)//選擇 區域 節點 的事件反應
        {
            Tree_Node tmp = (Tree_Node)tvmSub0001_02.SelectedNode;
            if (tmp != null)
            {
                tmp.Expand();
                initltvSub0001_01();//顯示對應區域下的門區資料
            }

        }

        private void tvmSub0001_02_MouseUp(object sender, MouseEventArgs e)
        {
            if (e.Button == MouseButtons.Right)
            {
                if (tvmSub0001_02.SelectedNode != null)
                {
                    Tree_Node tmp_Node = (Tree_Node)tvmSub0001_02.SelectedNode;
                    modifyAreaName(tmp_Node);
                }
            }
            else
            {
                //---
                //修正區域門區群組建立後無法跳至最上層，除非新增區域在取消
                tvmSub0001_02.SelectedNode = null;
                initltvSub0001_01();
                //---修正區域門區群組建立後無法跳至最上層，除非新增區域在取消
            }
        }

        private void ltvSub0001_01_SelectedIndexChanged(object sender, EventArgs e)//選擇要門區事件
        {

        }

        //Sub0001_end
        //Sub0002_start
        private void dgvSub0002_01_DoubleClick(object sender, EventArgs e)//at 2017/09/15
        {
            butSub0002_01.PerformClick();
        }
        public int m_intdgvSub0002_01_id = -1;
        private void dgvSub0002_01_SelectionChanged(object sender, EventArgs e)
        {
            //---
            //列表元件shift全選時最前面的狀態要能自動打勾，反之要能取消
            for (int i = 0; i < dgvSub0002_01.Rows.Count; i++)
            {
                dgvSub0002_01.Rows[i].Cells[0].Value = false;
            }
            for (int j = 0; j < dgvSub0002_01.SelectedRows.Count; j++)
            {
                dgvSub0002_01.SelectedRows[j].Cells[0].Value = true;
            }
            //---列表元件shift全選時最前面的狀態要能自動打勾，反之要能取消

            try
            {
                int index = dgvSub0002_01.SelectedRows[0].Index;//取得被選取的第一列位置
                String Strid = dgvSub0002_01.Rows[index].Cells[1].Value.ToString();
                m_intdgvSub0002_01_id = Int32.Parse(Strid);
            }
            catch
            {
            }
        }
        private void butSub0002_01_Click(object sender, EventArgs e)//編輯門區群組
        {
            //MessageBox.Show(m_intdgvSub0002_01_id + "");
            try
            {
                int index = dgvSub0002_01.SelectedRows[0].Index;//取得被選取的第一列位置
                String Strid = dgvSub0002_01.Rows[index].Cells[1].Value.ToString();
                m_intdgvSub0002_01_id = Int32.Parse(Strid);
            }
            catch
            {
            }
            if (m_intdgvSub0002_01_id > 0)
            {
                m_TPOld = m_tabMain.SelectedTab;//--2017/02/22 製作返回按鈕功能

                TabPage_Push();//m_StackTPOld.Push(m_TPOld);//--2017/02/22 製作返回按鈕功能
                m_tabSub000200.Parent = m_tabMain;
                initSub000200UI();
                m_tabMain.SelectedTab = m_tabSub000200;

                //--
                //同步子頁的選擇列表 at 2017/07/11
                for (int i = 0; i < dgvSub000200_01.Rows.Count; i++)
                {
                    int id = Convert.ToInt32(dgvSub000200_01.Rows[i].Cells[1].Value.ToString());
                    if (id != m_intdgvSub0002_01_id)
                    {
                        dgvSub000200_01.Rows[i].Selected = false;
                    }
                    else
                    {
                        dgvSub000200_01.Rows[i].Selected = true;
                    }
                }
                //--
                DB2LeftSub000200UI(m_intdgvSub0002_01_id);
                txtSub000200_01.Focus();//--2017/03/30 頁面切換後，指定該頁面特定元件取的焦點(Focus)

                m_Sub000200ALInit.Clear();//add at 2017/10/06
                m_Sub000200ALInit.Add(txtSub000200_01.Text);//add at 2017/10/06
                m_Sub000200ALInit.Add(ckbSub000200_01.Checked.ToString());//add at 2017/10/06
                m_Sub000200ALInit.Add(ckbSub000200_02.Checked.ToString());//add at 2017/10/06
                m_Sub000200ALInit.Add(adpSub000200_01.Value.ToString("yyyy-MM-dd HH:mm"));//add at 2017/10/06
                m_Sub000200ALInit.Add(adpSub000200_02.Value.ToString("yyyy-MM-dd HH:mm"));//add at 2017/10/06
                m_Sub000200ALInit.Add(rdbSub000200_01.Checked.ToString());//add at 2017/10/06
                m_Sub000200ALInit.Add(rdbSub000200_02.Checked.ToString());//add at 2017/10/06
                m_Sub000200ALInit.Add(rdbSub000200_03.Checked.ToString());//add at 2017/10/06
                m_Sub000200ALInit.Add(rdbSub000200_04.Checked.ToString());//add at 2017/10/06
                getTreeView(tvmSub000200_01);//add at 2017/10/06
                for (int i = 0; i < m_ALdoor_group_detail.Count; i++)
                {
                    m_Sub000200ALInit.Add(m_ALdoor_group_detail[i].ToString());//add at 2017/10/06
                }
            }
            //m_TPOld.Parent = null;//隱藏目前分頁，但是要分開寫，不可抓到就直接執行，必須先指定目前新分頁，否則系統會產生錯亂-2017/03/02
        }

        private void butSub0002_02_Click(object sender, EventArgs e)//新增門區群組
        {
            m_TPOld = m_tabMain.SelectedTab;//--2017/02/22 製作返回按鈕功能
            
            TabPage_Push();//m_StackTPOld.Push(m_TPOld);//--2017/02/22 製作返回按鈕功能
            m_tabSub000200.Parent = m_tabMain;
            initSub000200UI();
            m_tabMain.SelectedTab = m_tabSub000200;
            initLeftSub000200UI();

            //---
            //新增所有群組時都預設填入名稱
            //txtSub000200_01.Focus();//--2017/03/30 頁面切換後，指定該頁面特定元件取的焦點(Focus)
            txtSub000200_01.Text = "group_area_" + DateTime.Now.ToString("yyyyMMddHHmmss");
            //---新增所有群組時都預設填入名稱

            m_Sub000200ALInit.Clear();//add at 2017/10/06
            m_Sub000200ALInit.Add(txtSub000200_01.Text);//add at 2017/10/06
            m_Sub000200ALInit.Add(ckbSub000200_01.Checked.ToString());//add at 2017/10/06
            m_Sub000200ALInit.Add(ckbSub000200_02.Checked.ToString());//add at 2017/10/06
            m_Sub000200ALInit.Add(adpSub000200_01.Value.ToString("yyyy-MM-dd HH:mm"));//add at 2017/10/06
            m_Sub000200ALInit.Add(adpSub000200_02.Value.ToString("yyyy-MM-dd HH:mm"));//add at 2017/10/06
            m_Sub000200ALInit.Add(rdbSub000200_01.Checked.ToString());//add at 2017/10/06
            m_Sub000200ALInit.Add(rdbSub000200_02.Checked.ToString());//add at 2017/10/06
            m_Sub000200ALInit.Add(rdbSub000200_03.Checked.ToString());//add at 2017/10/06
            m_Sub000200ALInit.Add(rdbSub000200_04.Checked.ToString());//add at 2017/10/06

            //m_TPOld.Parent = null;//隱藏目前分頁，但是要分開寫，不可抓到就直接執行，必須先指定目前新分頁，否則系統會產生錯亂-2017/03/02
        }

        private void butSub0002_06_Click(object sender, EventArgs e)//門區群組全選
        {
            /*
            for (int i = 0; i < dgvSub0002_01.Rows.Count; i++)
            {
                dgvSub0002_01.Rows[i].Cells[0].Value = true;
                dgvSub0002_01.Rows[i].Selected = true;
            }
            */
            dgvSub0002_01.SelectAll();//把所有列表元件的全選/取消全選改成SelectAll()/ClearSelection()
        }

        private void butSub0002_07_Click(object sender, EventArgs e)//門區群組取消全選
        {
            /*
            for (int i = 0; i < dgvSub0002_01.Rows.Count; i++)
            {
                dgvSub0002_01.Rows[i].Cells[0].Value = false;
                dgvSub0002_01.Rows[i].Selected = false;
            }
            */
            dgvSub0002_01.ClearSelection();//把所有列表元件的全選/取消全選改成SelectAll()/ClearSelection()
        }

        private void butSub0002_08_Click(object sender, EventArgs e)//門區群組批次執行
        {
            ArrayList ALSN = new ArrayList();
            ALSN.Clear();
            for (int i = 0; i < dgvSub0002_01.Rows.Count; i++)
            {
                String data = dgvSub0002_01.Rows[i].Cells[0].Value.ToString().ToLower();//抓取DataGridView欄位資料
                if (data == "true")
                {
                    ALSN.Add(dgvSub0002_01.Rows[i].Cells[1].Value.ToString());//抓 ID
                }
            }
            String SQL = "";
            switch (cmbSub0002_01.SelectedIndex)
            {
                case 0:
                    SQL = "";
                    for (int i = 0; i < ALSN.Count; i++)
                    {
                        SQL += "UPDATE door_group SET enable = 1,state=1 WHERE (id = " + ALSN[i].ToString() + ");";
                    }

                    break;
                case 1:
                    SQL = "";
                    for (int i = 0; i < ALSN.Count; i++)
                    {
                        SQL += "UPDATE door_group SET enable = 0,state=1 WHERE (id = " + ALSN[i].ToString() + ");";
                    }
                    break;
                case 2:
                    SQL = "";
                    for (int i = 0; i < ALSN.Count; i++)
                    {
                        SQL += String.Format("DELETE FROM door_group WHERE id={0};DELETE FROM door_group_detail WHERE door_group_id={0};", ALSN[i].ToString());
                    }
                    break;
            }
            MySQL.InsertUpdateDelete(SQL);//新增資料程式

            initdgvSub0002_01();
        }

        private void butSub0002_09_Click(object sender, EventArgs e)//門區群組列表的搜尋功能
        {
            initdgvSub0002_01();
            ArrayList AL01 = new ArrayList();
            ArrayList AL02 = new ArrayList();
            ArrayList AL03 = new ArrayList();
            ArrayList AL04 = new ArrayList();
            ArrayList AL05 = new ArrayList();
            AL01.Clear();
            AL02.Clear();
            AL03.Clear();
            AL04.Clear();
            AL05.Clear();

            if (txtSub0002_01.Text != "")
            {
                for (int i = 0; i < dgvSub0002_01.Rows.Count; i++)//取的現行UI上控制器列表所有資料
                {
                    AL01.Add(dgvSub0002_01.Rows[i].Cells[1].Value.ToString());
                    AL02.Add(dgvSub0002_01.Rows[i].Cells[2].Value.ToString());
                    AL03.Add(dgvSub0002_01.Rows[i].Cells[3].Value.ToString());
                    AL04.Add(dgvSub0002_01.Rows[i].Cells[4].Value.ToString());
                    AL05.Add(dgvSub0002_01.Rows[i].Cells[5].Value.ToString());
                }
                try
                {
                    //--
                    //dgvSub0002_01.ReadOnly = true;//唯讀 不可更改
                    dgvSub0002_01.RowHeadersVisible = false;//DataGridView 最前面指示選取列所在位置的箭頭欄位
                    dgvSub0002_01.Rows[0].Selected = false;//取消DataGridView的默認選取(選中)Cell 使其不反藍
                    dgvSub0002_01.AllowUserToAddRows = false;//是否允許使用者新增資料
                    dgvSub0002_01.AllowUserToDeleteRows = false;//是否允許使用者刪除資料
                    dgvSub0002_01.AllowUserToOrderColumns = false;//是否允許使用者調整欄位位置
                    //所有表格欄位寬度全部變成可調 dgvSub0002_01.AllowUserToResizeColumns = false;//是否允許使用者改變欄寬
                    dgvSub0002_01.AllowUserToResizeRows = false;//是否允許使用者改變行高
                    dgvSub0002_01.Columns[1].ReadOnly = true;//單一欄位禁止編輯
                    dgvSub0002_01.Columns[2].ReadOnly = true;//單一欄位禁止編輯
                    dgvSub0002_01.Columns[3].ReadOnly = true;//單一欄位禁止編輯
                    dgvSub0002_01.Columns[4].ReadOnly = true;//單一欄位禁止編輯
                    dgvSub0002_01.Columns[5].ReadOnly = true;//單一欄位禁止編輯
                    dgvSub0002_01.AllowUserToAddRows = false;//刪除空白列
                    dgvSub0002_01.SelectionMode = System.Windows.Forms.DataGridViewSelectionMode.FullRowSelect;//整列選取
                                                                                                               //--

                    do
                    {
                        for (int i = 0; i < dgvSub0002_01.Rows.Count; i++)
                        {
                            DataGridViewRow r1 = this.dgvSub0002_01.Rows[i];//取得DataGridView整列資料
                            this.dgvSub0002_01.Rows.Remove(r1);//DataGridView刪除整列
                        }
                    } while (dgvSub0002_01.Rows.Count > 0);

                }
                catch
                {
                }
                String StrSearch = txtSub0002_01.Text;
                for (int i = 0; i < AL01.Count; i++)
                {
                    //AL01[i].ToString()->DB index 本來就被隱藏 所以不用在搜尋欄位內
                    if ((AL02[i].ToString().IndexOf(StrSearch) > -1) || (AL03[i].ToString().IndexOf(StrSearch) > -1) || (AL04[i].ToString().IndexOf(StrSearch) > -1) || (AL05[i].ToString().IndexOf(StrSearch) > -1))
                    {
                        this.dgvSub0002_01.Rows.Add(false, AL01[i].ToString(), AL02[i].ToString(), AL03[i].ToString(), AL04[i].ToString(), AL05[i].ToString());
                    }
                }
            }
        }

        private void ckbSub0002_01_CheckedChanged(object sender, EventArgs e)//門區群組已啟用/已停用 篩選事件
        {
            m_StrdgvSub0002_01_ext01 = "";
            if (ckbSub0002_01.Checked)
            {
                if (ckbSub0002_02.Checked)//(1,1)
                {
                    m_StrdgvSub0002_01_ext01 = "";//兩個都選等於沒選
                }
                else//(1,0)
                {
                    m_StrdgvSub0002_01_ext01 = " WHERE enable = 1";
                }
            }
            else
            {
                if (ckbSub0002_02.Checked)//(0,1)
                {
                    m_StrdgvSub0002_01_ext01 = " WHERE enable = 0";
                }
                else//(0,0)
                {
                    m_StrdgvSub0002_01_ext01 = "";//沒選
                }
            }
            initdgvSub0002_01();
        }

        //Sub0002_end
        //Sub000001_start
        private int[,] m_intSub000001_Date = new int[12, 31];
        private int[] m_intSub000001_Date_value = new int[12];
        private ArrayList m_ALSub000001Name = new ArrayList();
        private ArrayList m_ALSub000001Date = new ArrayList();
        private ArrayList m_ALSub000001State = new ArrayList();
        private void m_tabSub000001_Leave(object sender, EventArgs e)//偵測要從編輯控制器離開時的事件 at 2017/06/29
        {
            get_show_Controllers(false); //控制器編修完成後要呼叫控制器列表刷新狀態(fasle<->true)	get_show_Controllers();//取的控制器列表
        }

        private void butSub000001_01_Click(object sender, EventArgs e)//手動新增假日到DataGridView
        {
            String StrName = txtSub000001_05.Text;
            String StrDate = String.Format("{0:00}/{1:00}", dtpSub000001_01.Value.Month, dtpSub000001_01.Value.Day);
            m_intSub000001_Date[dtpSub000001_01.Value.Month-1, dtpSub000001_01.Value.Day-1] = 1;

            this.dgvSub000001_01.Rows.Add(false, StrDate, StrName,"Enable");

            m_ALSub000001Name.Clear();
            m_ALSub000001Date.Clear();
            m_ALSub000001State.Clear();
            for (int i = 0; i < dgvSub000001_01.Rows.Count; i++)
            {
                String date = dgvSub000001_01.Rows[i].Cells[1].Value.ToString();//抓取DataGridView欄位資料
                String name = dgvSub000001_01.Rows[i].Cells[2].Value.ToString();//抓取DataGridView欄位資料
                String state = dgvSub000001_01.Rows[i].Cells[3].Value.ToString();//抓取DataGridView欄位資料
                m_ALSub000001Name.Add(name);
                m_ALSub000001Date.Add(date);
                m_ALSub000001State.Add(state);
            }
        }

        //---
        //按照『V8 功能選單』一個一個改 - 控制器編輯 ~ 下拉是選單 啟用/停用/刪除 控制器 變成獨立按鈕
        private void butSub000001_04_Click(object sender, EventArgs e)//假日啟用
        {
            BatchHolidayAction(1);
        }

        private void butSub000001_15_Click(object sender, EventArgs e)//假日停用
        {
            BatchHolidayAction(2);
        }

        private void butSub000001_06_Click(object sender, EventArgs e)//假日刪除
        {
            BatchHolidayAction(0);
        }

        public void BatchHolidayAction(int step)
        {
            switch (step)//switch (cmbSub000001_02.SelectedIndex)
            {
                case 0:
                    deleteSelectsdgvSub000001_01();
                    break;
                case 1:
                    enableSelectsdgvSub000001_01();
                    break;
                case 2:
                    disableSelectsdgvSub000001_01();
                    break;
            }

            m_ALSub000001Name.Clear();
            m_ALSub000001Date.Clear();
            m_ALSub000001State.Clear();
            for (int i = 0; i < dgvSub000001_01.Rows.Count; i++)
            {
                String date = dgvSub000001_01.Rows[i].Cells[1].Value.ToString();//抓取DataGridView欄位資料
                String name = dgvSub000001_01.Rows[i].Cells[2].Value.ToString();//抓取DataGridView欄位資料
                String state = dgvSub000001_01.Rows[i].Cells[3].Value.ToString();//抓取DataGridView欄位資料
                m_ALSub000001Name.Add(name);
                m_ALSub000001Date.Add(date);
                m_ALSub000001State.Add(state);
            }
        }
        //---按照『V8 功能選單』一個一個改 - 控制器編輯 ~ 下拉是選單 啟用/停用/刪除 控制器 變成獨立按鈕

        public bool m_blnHSelectAll = true;//按照『V8 功能選單』一個一個改 - 控制器編輯 ~ 全選/取消全選 整合成同一個按鈕
        private void butSub000001_05_Click(object sender, EventArgs e)//假日列表全選+取消全選
        {
            //---
            //按照『V8 功能選單』一個一個改 - 控制器編輯 ~ 全選/取消全選 整合成同一個按鈕
            if (m_blnHSelectAll == true)
            {
                dgvSub000001_01.SelectAll();
                butSub000001_05.ImageIndex = 7;
                butSub000001_05.Text = Language.m_StrbutSub000001_05_02;
            }
            else
            {
                dgvSub000001_01.ClearSelection();//把所有列表元件的全選/取消全選改成SelectAll()/ClearSelection()
                butSub000001_05.ImageIndex = 8;
                butSub000001_05.Text = Language.m_StrbutSub000001_05_01;
            }
            m_blnHSelectAll = (!m_blnHSelectAll);
            //---按照『V8 功能選單』一個一個改 - 控制器編輯 ~ 全選/取消全選 整合成同一個按鈕
        }

        public void deleteSelectsdgvSub000001_01()//假日列表刪除全選
        {
            for (int i = 0; i < dgvSub000001_01.Rows.Count; i++)
            {
                String data = dgvSub000001_01.Rows[i].Cells[0].Value.ToString().ToLower();//抓取DataGridView欄位資料
                if (data == "true")
                {
                    String[] date = dgvSub000001_01.Rows[i].Cells[1].Value.ToString().ToLower().Split('/'); ;
                    m_intSub000001_Date[Int32.Parse(date[0]) - 1, Int32.Parse(date[1]) - 1] = 0;
                    DataGridViewRow r1 = this.dgvSub000001_01.Rows[i];//取得DataGridView整列資料
                    this.dgvSub000001_01.Rows.Remove(r1);//DataGridView刪除整列
                }
            }
        }

        public void enableSelectsdgvSub000001_01()//假日列表啟用選擇
        {
            for (int i = 0; i < dgvSub000001_01.Rows.Count; i++)
            {
                String data = dgvSub000001_01.Rows[i].Cells[0].Value.ToString().ToLower();//抓取DataGridView欄位資料
                if (data == "true")
                {
                    dgvSub000001_01.Rows[i].Cells[3].Value = "Enable";
                }
            }
        }

        public void disableSelectsdgvSub000001_01()//假日列表停用選擇
        {
            for (int i = 0; i < dgvSub000001_01.Rows.Count; i++)
            {
                String data = dgvSub000001_01.Rows[i].Cells[0].Value.ToString().ToLower();//抓取DataGridView欄位資料
                if (data == "true")
                {
                    dgvSub000001_01.Rows[i].Cells[3].Value = "Disable";
                }
            }
        }

        private void butSub000001_02_Click(object sender, EventArgs e)//載入檔案
        {
            String StrPath = "";
            String StrDate, StrName,StrState;
            OpenFileDialog openFileDialog1 = new OpenFileDialog();
            openFileDialog1.Filter = "CSV File|*.csv";
            openFileDialog1.Title = "Open an CSV";
            openFileDialog1.RestoreDirectory = true;
            if (openFileDialog1.ShowDialog() == DialogResult.OK)
            {
                StrPath = openFileDialog1.FileName.ToString();
                // 建立檔案串流（@ 可取消跳脫字元 escape sequence）
                StreamReader sr = new StreamReader(StrPath);
                while (!sr.EndOfStream)// 每次讀取一行，直到檔尾
                {
                    String line = sr.ReadLine();// 讀取文字到 line 變數

                    string[] strs = line.Split(',');
                    if (strs.Length > 2)
                    {
                        StrName = strs[1];
                        StrDate = strs[0];
                        StrState = strs[2];
                    }
                    else
                    {
                        StrState = "";
                        StrName="";
                        StrDate=strs[0];
                    }
                    String[] date = StrDate.Split('/'); ;
                    m_intSub000001_Date[Int32.Parse(date[0]) - 1, Int32.Parse(date[1]) - 1] = 1;
                    this.dgvSub000001_01.Rows.Add(false, StrDate, StrName, StrState);
                }
                sr.Close();// 關閉串流

                m_ALSub000001Name.Clear();
                m_ALSub000001Date.Clear();
                m_ALSub000001State.Clear();
                for (int i = 0; i < dgvSub000001_01.Rows.Count; i++)
                {
                    String date = dgvSub000001_01.Rows[i].Cells[1].Value.ToString();//抓取DataGridView欄位資料
                    String name = dgvSub000001_01.Rows[i].Cells[2].Value.ToString();//抓取DataGridView欄位資料
                    String state = dgvSub000001_01.Rows[i].Cells[3].Value.ToString();//抓取DataGridView欄位資料
                    m_ALSub000001Name.Add(name);
                    m_ALSub000001Date.Add(date);
                    m_ALSub000001State.Add(state);
                }
            }
        }

        private void butSub000001_03_Click(object sender, EventArgs e)//寫入檔案
        {
            String StrPath = "";
            SaveFileDialog saveFileDialog1 = new SaveFileDialog();
            saveFileDialog1.Filter = "CSV File|*.csv";
            saveFileDialog1.Title = "Save an CSV";
            saveFileDialog1.FileName = "Holiday.csv";
            if (saveFileDialog1.ShowDialog() == DialogResult.OK)
            {
                StrPath = saveFileDialog1.FileName.ToString();
                StreamWriter sw = new StreamWriter(StrPath, false, System.Text.Encoding.UTF8);
                for (int i = 0; i < dgvSub000001_01.Rows.Count; i++)
                {
                    String date = dgvSub000001_01.Rows[i].Cells[1].Value.ToString();//抓取DataGridView欄位資料
                    String name = dgvSub000001_01.Rows[i].Cells[2].Value.ToString();//抓取DataGridView欄位資料
                    String state = dgvSub000001_01.Rows[i].Cells[3].Value.ToString();//抓取DataGridView欄位資料
                    String Data = date + "," + name + "," + state;
                    sw.WriteLine(Data);
                }
                sw.Close();
            }
        }

        private void butSub000001_07_Click(object sender, EventArgs e)//搜尋
        {
            ArrayList ALIndex = new ArrayList();
            ALIndex.Clear();

            dgvSub000001_01.Rows.Clear();//Datagridview 清空

            if (txtSub000001_06.Text=="")
            {
                for (int i = 0; i < m_ALSub000001Name.Count; i++)
                {
                    this.dgvSub000001_01.Rows.Add(false, m_ALSub000001Date[i].ToString(), m_ALSub000001Name[i].ToString(),m_ALSub000001State[i].ToString());
                }
            }
            else
            {
                String StrSearch = txtSub000001_06.Text;
                for(int i=0;i < m_ALSub000001Name.Count; i++)
                {
                    if ((m_ALSub000001Name[i].ToString().IndexOf(StrSearch) > -1) || (m_ALSub000001Date[i].ToString().IndexOf(StrSearch) > -1) || (m_ALSub000001State[i].ToString().IndexOf(StrSearch) > -1))
                    {
                        ALIndex.Add(i);
                    }
                }

                ALIndex = GetDistinctArray(ALIndex);
                for (int i=0;i < ALIndex.Count;i++)
                {
                    int j = Int32.Parse(ALIndex[i].ToString());
                    this.dgvSub000001_01.Rows.Add(false, m_ALSub000001Date[j].ToString(), m_ALSub000001Name[j].ToString(),m_ALSub000001State[j].ToString());
                }
            }
        }
        public bool ControlData2DB(bool blnSaveDB=true,int intState=1)
        {
            bool blnAns = true;
            String Strsn, Strname, Stralias, Strmodel, Strstate;//controller
            String Strcontroller_sn, Strconnetction_address, Strconnetction_enabled, Strconnetction_mode, Strapb_enable, Strapb_mode, Strapb_group, Strapb_level_list, Strapb_reset_timestamp_list, Strab_door_enabled, Strab_door_level, Strab_door_timeout_second, Strab_door_reset_time_second, Strport, Strdoor_number, Strsame_card_interval_time_second;//controll_extend
            String Strsydm_id = "0";//修改 儲存控制器時支援 SYDM 選擇元件

            Strmodel = "0";
            Strab_door_timeout_second = "0";
            Strab_door_reset_time_second = "0";

            //--
            //add at 2017/10/05
            if ((!blnSaveDB) && (cmbSub000001_03.SelectedIndex == -1))
            {
                m_Sub000001ALData.Clear();

                m_Sub000001ALData.Add(txtSub000001_01.Text);//add at 2017/10/05
                m_Sub000001ALData.Add(txtSub000001_02.Text);//add at 2017/10/05
                m_Sub000001ALData.Add(labSub000001_09.Text);//add at 2017/10/05
                m_Sub000001ALData.Add(txtSub000001_03.Text);//add at 2017/10/05
                m_Sub000001ALData.Add(txtSub000001_04.Text);//add at 2017/10/05
                m_Sub000001ALData.Add(rdbSub000001_01.Checked.ToString());//add at 2017/10/05
                m_Sub000001ALData.Add(ckbSub000001_01.Checked.ToString());//add at 2017/10/05
                m_Sub000001ALData.Add(rdbSub000001_03.Checked.ToString());//add at 2017/10/05
                m_Sub000001ALData.Add(rdbSub000001_04.Checked.ToString());//add at 2017/10/05
                m_Sub000001ALData.Add(ckbSub000001_02.Checked.ToString());//add at 2017/10/05
                m_Sub000001ALData.Add(txtSub000001_19.Value + "");//add at 2017/10/05
                m_Sub000001ALData.Add(cmbSub000001_01.SelectedIndex + "");//add at 2017/10/05
                m_Sub000001ALData.Add(cmbSub000001_03.SelectedIndex + "");//add at 2017/10/05
                m_Sub000001ALData.Add(cmbSub000001_04.SelectedIndex + "");//SYDM 選擇元件 修改偵測
                return CheckUIVarNotChange(m_Sub000001ALInit, m_Sub000001ALData);
            }
            //--

            //--
            //controller
            Strname = txtSub000001_01.Text;//名稱
            Stralias = txtSub000001_02.Text;//別名
            if (txtSub000001_04.Text.Length <= 4)//IP防呆
            {
                txtSub000001_04.Text = "192.168.0.1";
            }

            if ((HW_Net_API.m_ALHW_ID.Count > 0) && (cmbSub000001_03.SelectedIndex >= 0))
            {
                Strmodel = HW_Net_API.m_ALHW_ID[cmbSub000001_03.SelectedIndex].ToString();
                Strmodel = "" + Convert.ToInt32(Strmodel, 16);//型號
                Strdoor_number = HW_Net_API.m_ALHW_DoorNumber[cmbSub000001_03.SelectedIndex].ToString();//門數
                labSub000001_03.ForeColor = Color.Black;
            }
            else
            {
                labSub000001_03.ForeColor = Color.Red;
                blnAns = false;
                return blnAns;
            }

            if (txtSub000001_01.Text != "")//名稱
            {
                labSub000001_01.ForeColor = Color.Black;
            }
            else
            {
                labSub000001_01.ForeColor = Color.Red;
                blnAns = false;
                return blnAns;
            }

            if (labSub000001_09.Text != "")
            {
                Strsn = labSub000001_09.Text;//序號
                labSub000001_04.ForeColor = Color.Black;
            }
            else
            {
                labSub000001_04.ForeColor = Color.Red;
                blnAns = false;
                return blnAns;
            }

            //---
            //SYCG模式下新增控制器一定要有SYDM的防呆機制
            if (m_changeSYCGMode)
            {
                if (cmbSub000001_04.SelectedIndex == -1)
                {
                    labSub000001_20.ForeColor = Color.Red;
                    blnAns = false;
                    return blnAns;
                }
            }
            //---SYCG模式下新增控制器一定要有SYDM的防呆機制

            Strstate = ""+intState;//是否要更新sy_dm狀態
            //識別碼-->?
            //--

            //--
            //controll_extend
            Strsame_card_interval_time_second = "" + Convert.ToInt32(txtSub000001_07.Text);//add 2017/08/22
            if (txtSub000001_03.Text.Length <= 0)//port 防呆
            {
                txtSub000001_03.Text = "5001";
            }
            Strport = txtSub000001_03.Text;

            Strcontroller_sn = labSub000001_09.Text;//序號
            Strconnetction_address = txtSub000001_04.Text;//IP
            if (rdbSub000001_01.Checked)//控制器狀態
            {
                Strconnetction_enabled = "1";
            }
            else
            {
                Strconnetction_enabled = "0";
            }
            if (cmbSub000001_01.SelectedIndex > -1)//連線方式
            {
                Strconnetction_mode = "" + cmbSub000001_01.SelectedIndex;
            }
            else
            {
                cmbSub000001_01.SelectedIndex = 0;
                Strconnetction_mode = "" + cmbSub000001_01.SelectedIndex;
            }

            if (ckbSub000001_01.Checked)
            {
                Strapb_enable = "1";
            }
            else
            {
                Strapb_enable = "0";
            }

            Strapb_mode = "0";
            if (rdbSub000001_03.Checked)
            {
                Strapb_mode = "1";
            }
            else if (rdbSub000001_04.Checked)
            {
                Strapb_mode = "2";
            }

            Strapb_group = "0";
            Strapb_level_list = "0";
            Strapb_reset_timestamp_list = "0";
            if (ckbSub000001_02.Checked)
            {
                Strab_door_enabled = "1";
            }
            else
            {
                Strab_door_enabled = "0";
            }
            if (Strab_door_enabled == "0")
            {
                Strab_door_level = "0";
            }
            else
            {
                Strab_door_level = "" + txtSub000001_19.Value;
            }
            if ((Strab_door_level == "0") || (Strab_door_level == "1"))
            {
                Strab_door_timeout_second = "0";
                Strab_door_reset_time_second = "0";
            }
            else
            {
                if (Convert.ToInt32(Strab_door_level) > 1)
                {
                    if (AB12LEVEL4Sub000001_01.Visible)
                    {
                        Strab_door_timeout_second = "" + AB12LEVEL4Sub000001_01.jlNumEdit1.Value;
                        Strab_door_reset_time_second = "" + AB12LEVEL4Sub000001_01.jlNumEdit2.Value;
                    }

                    if (AB12LEVEL3Sub000001_01.Visible)
                    {
                        Strab_door_timeout_second = "" + AB12LEVEL3Sub000001_01.jlNumEdit1.Value;
                        Strab_door_reset_time_second = "" + AB12LEVEL3Sub000001_01.jlNumEdit2.Value;
                    }
                    if (AB12LEVEL2Sub000001_01.Visible)
                    {
                        Strab_door_timeout_second = "" + AB12LEVEL2Sub000001_01.jlNumEdit1.Value;
                        Strab_door_reset_time_second = "" + AB12LEVEL2Sub000001_01.jlNumEdit2.Value;
                    }
                    if (AB4LEVEL4Sub000001_01.Visible)
                    {
                        Strab_door_timeout_second = "" + AB4LEVEL4Sub000001_01.jlNumEdit1.Value;
                        Strab_door_reset_time_second = "" + AB4LEVEL4Sub000001_01.jlNumEdit2.Value;
                    }
                    if (AB4LEVEL3Sub000001_01.Visible)
                    {
                        Strab_door_timeout_second = "" + AB4LEVEL3Sub000001_01.jlNumEdit1.Value;
                        Strab_door_reset_time_second = "" + AB4LEVEL3Sub000001_01.jlNumEdit2.Value;
                    }
                    if (AB4LEVEL2Sub000001_01.Visible)
                    {
                        Strab_door_timeout_second = "" + AB4LEVEL2Sub000001_01.jlNumEdit1.Value;
                        Strab_door_reset_time_second = "" + AB4LEVEL2Sub000001_01.jlNumEdit2.Value;
                    }
                }
            }
            //--
            //door
            m_ALDoors.Clear();
            int intdoornum = Convert.ToInt32(HW_Net_API.m_ALHW_DoorNumber[cmbSub000001_03.SelectedIndex].ToString());
            if (ckbSub000001_02.Checked)
            {
                if (AB12LEVEL4Sub000001_01.Visible)
                {
                    m_ALDoors.Add(AB12LEVEL4Sub000001_01.textBox1.Text);
                    m_ALDoors.Add(AB12LEVEL4Sub000001_01.textBox2.Text);
                    m_ALDoors.Add(AB12LEVEL4Sub000001_01.textBox3.Text);
                    m_ALDoors.Add(AB12LEVEL4Sub000001_01.textBox4.Text);
                    m_ALDoors.Add(AB12LEVEL4Sub000001_01.textBox5.Text);
                    m_ALDoors.Add(AB12LEVEL4Sub000001_01.textBox6.Text);
                    m_ALDoors.Add(AB12LEVEL4Sub000001_01.textBox7.Text);
                    m_ALDoors.Add(AB12LEVEL4Sub000001_01.textBox8.Text);
                    m_ALDoors.Add(AB12LEVEL4Sub000001_01.textBox9.Text);
                    m_ALDoors.Add(AB12LEVEL4Sub000001_01.textBox10.Text);
                    m_ALDoors.Add(AB12LEVEL4Sub000001_01.textBox11.Text);
                    m_ALDoors.Add(AB12LEVEL4Sub000001_01.textBox12.Text);
                }
                if (AB12LEVEL3Sub000001_01.Visible)
                {
                    m_ALDoors.Add(AB12LEVEL3Sub000001_01.textBox1.Text);
                    m_ALDoors.Add(AB12LEVEL3Sub000001_01.textBox2.Text);
                    m_ALDoors.Add(AB12LEVEL3Sub000001_01.textBox3.Text);
                    m_ALDoors.Add(AB12LEVEL3Sub000001_01.textBox4.Text);
                    m_ALDoors.Add(AB12LEVEL3Sub000001_01.textBox5.Text);
                    m_ALDoors.Add(AB12LEVEL3Sub000001_01.textBox6.Text);
                    m_ALDoors.Add(AB12LEVEL3Sub000001_01.textBox7.Text);
                    m_ALDoors.Add(AB12LEVEL3Sub000001_01.textBox8.Text);
                    m_ALDoors.Add(AB12LEVEL3Sub000001_01.textBox9.Text);
                    m_ALDoors.Add(AB12LEVEL3Sub000001_01.textBox10.Text);
                    m_ALDoors.Add(AB12LEVEL3Sub000001_01.textBox11.Text);
                    m_ALDoors.Add(AB12LEVEL3Sub000001_01.textBox12.Text);
                }
                if (AB12LEVEL2Sub000001_01.Visible)
                {
                    m_ALDoors.Add(AB12LEVEL2Sub000001_01.textBox1.Text);
                    m_ALDoors.Add(AB12LEVEL2Sub000001_01.textBox2.Text);
                    m_ALDoors.Add(AB12LEVEL2Sub000001_01.textBox3.Text);
                    m_ALDoors.Add(AB12LEVEL2Sub000001_01.textBox4.Text);
                    m_ALDoors.Add(AB12LEVEL2Sub000001_01.textBox5.Text);
                    m_ALDoors.Add(AB12LEVEL2Sub000001_01.textBox6.Text);
                    m_ALDoors.Add(AB12LEVEL2Sub000001_01.textBox7.Text);
                    m_ALDoors.Add(AB12LEVEL2Sub000001_01.textBox8.Text);
                    m_ALDoors.Add(AB12LEVEL2Sub000001_01.textBox9.Text);
                    m_ALDoors.Add(AB12LEVEL2Sub000001_01.textBox10.Text);
                    m_ALDoors.Add(AB12LEVEL2Sub000001_01.textBox11.Text);
                    m_ALDoors.Add(AB12LEVEL2Sub000001_01.textBox12.Text);
                }
                if (AB12LEVEL1Sub000001_01.Visible)
                {
                    m_ALDoors.Add(AB12LEVEL1Sub000001_01.textBox1.Text);
                    m_ALDoors.Add(AB12LEVEL1Sub000001_01.textBox2.Text);
                    m_ALDoors.Add(AB12LEVEL1Sub000001_01.textBox3.Text);
                    m_ALDoors.Add(AB12LEVEL1Sub000001_01.textBox4.Text);
                    m_ALDoors.Add(AB12LEVEL1Sub000001_01.textBox5.Text);
                    m_ALDoors.Add(AB12LEVEL1Sub000001_01.textBox6.Text);
                    m_ALDoors.Add(AB12LEVEL1Sub000001_01.textBox7.Text);
                    m_ALDoors.Add(AB12LEVEL1Sub000001_01.textBox8.Text);
                    m_ALDoors.Add(AB12LEVEL1Sub000001_01.textBox9.Text);
                    m_ALDoors.Add(AB12LEVEL1Sub000001_01.textBox10.Text);
                    m_ALDoors.Add(AB12LEVEL1Sub000001_01.textBox11.Text);
                    m_ALDoors.Add(AB12LEVEL1Sub000001_01.textBox12.Text);
                }
                if (AB4LEVEL4Sub000001_01.Visible)
                {
                    m_ALDoors.Add(AB4LEVEL4Sub000001_01.textBox1.Text);
                    m_ALDoors.Add(AB4LEVEL4Sub000001_01.textBox2.Text);
                    m_ALDoors.Add(AB4LEVEL4Sub000001_01.textBox3.Text);
                    m_ALDoors.Add(AB4LEVEL4Sub000001_01.textBox4.Text);
                }
                if (AB4LEVEL3Sub000001_01.Visible)
                {
                    m_ALDoors.Add(AB4LEVEL3Sub000001_01.textBox1.Text);
                    m_ALDoors.Add(AB4LEVEL3Sub000001_01.textBox2.Text);
                    m_ALDoors.Add(AB4LEVEL3Sub000001_01.textBox3.Text);
                    m_ALDoors.Add(AB4LEVEL3Sub000001_01.textBox4.Text);
                }
                if (AB4LEVEL2Sub000001_01.Visible)
                {
                    m_ALDoors.Add(AB4LEVEL2Sub000001_01.textBox1.Text);
                    m_ALDoors.Add(AB4LEVEL2Sub000001_01.textBox2.Text);
                    m_ALDoors.Add(AB4LEVEL2Sub000001_01.textBox3.Text);
                    m_ALDoors.Add(AB4LEVEL2Sub000001_01.textBox4.Text);
                }
                if (AB4LEVEL1Sub000001_01.Visible)
                {
                    m_ALDoors.Add(AB4LEVEL1Sub000001_01.textBox1.Text);
                    m_ALDoors.Add(AB4LEVEL1Sub000001_01.textBox2.Text);
                    m_ALDoors.Add(AB4LEVEL1Sub000001_01.textBox3.Text);
                    m_ALDoors.Add(AB4LEVEL1Sub000001_01.textBox4.Text);
                }
            }
            else//一般(沒有A/B門)
            {
                switch (intdoornum)
                {
                    //--
                    //add 2017/10/19
                    case 128:
                        m_ALDoors.Clear();
                        egsSub000001_01.GetAllName();
                        for (int i = 0; i < egsSub000001_01.m_ALAllName.Count; i++)
                        {
                            m_ALDoors.Add(egsSub000001_01.m_ALAllName[i].ToString());
                        }
                        break;
                    //--
                    case 12:
                        m_ALDoors.Add(Door12Sub000001_01.txtDoor12_01.Text);
                        m_ALDoors.Add(Door12Sub000001_01.txtDoor12_02.Text);
                        m_ALDoors.Add(Door12Sub000001_01.txtDoor12_03.Text);
                        m_ALDoors.Add(Door12Sub000001_01.txtDoor12_04.Text);
                        m_ALDoors.Add(Door12Sub000001_01.txtDoor12_05.Text);
                        m_ALDoors.Add(Door12Sub000001_01.txtDoor12_06.Text);
                        m_ALDoors.Add(Door12Sub000001_01.txtDoor12_07.Text);
                        m_ALDoors.Add(Door12Sub000001_01.txtDoor12_08.Text);
                        m_ALDoors.Add(Door12Sub000001_01.txtDoor12_09.Text);
                        m_ALDoors.Add(Door12Sub000001_01.txtDoor12_10.Text);
                        m_ALDoors.Add(Door12Sub000001_01.txtDoor12_11.Text);
                        m_ALDoors.Add(Door12Sub000001_01.txtDoor12_12.Text);
                        break;
                    case 11:
                        m_ALDoors.Add(Door12Sub000001_01.txtDoor12_01.Text);
                        m_ALDoors.Add(Door12Sub000001_01.txtDoor12_02.Text);
                        m_ALDoors.Add(Door12Sub000001_01.txtDoor12_03.Text);
                        m_ALDoors.Add(Door12Sub000001_01.txtDoor12_04.Text);
                        m_ALDoors.Add(Door12Sub000001_01.txtDoor12_05.Text);
                        m_ALDoors.Add(Door12Sub000001_01.txtDoor12_06.Text);
                        m_ALDoors.Add(Door12Sub000001_01.txtDoor12_07.Text);
                        m_ALDoors.Add(Door12Sub000001_01.txtDoor12_08.Text);
                        m_ALDoors.Add(Door12Sub000001_01.txtDoor12_09.Text);
                        m_ALDoors.Add(Door12Sub000001_01.txtDoor12_10.Text);
                        m_ALDoors.Add(Door12Sub000001_01.txtDoor12_11.Text);
                        break;
                    case 10:
                        m_ALDoors.Add(Door12Sub000001_01.txtDoor12_01.Text);
                        m_ALDoors.Add(Door12Sub000001_01.txtDoor12_02.Text);
                        m_ALDoors.Add(Door12Sub000001_01.txtDoor12_03.Text);
                        m_ALDoors.Add(Door12Sub000001_01.txtDoor12_04.Text);
                        m_ALDoors.Add(Door12Sub000001_01.txtDoor12_05.Text);
                        m_ALDoors.Add(Door12Sub000001_01.txtDoor12_06.Text);
                        m_ALDoors.Add(Door12Sub000001_01.txtDoor12_07.Text);
                        m_ALDoors.Add(Door12Sub000001_01.txtDoor12_08.Text);
                        m_ALDoors.Add(Door12Sub000001_01.txtDoor12_09.Text);
                        m_ALDoors.Add(Door12Sub000001_01.txtDoor12_10.Text);
                        break;
                    case 09:
                        m_ALDoors.Add(Door12Sub000001_01.txtDoor12_01.Text);
                        m_ALDoors.Add(Door12Sub000001_01.txtDoor12_02.Text);
                        m_ALDoors.Add(Door12Sub000001_01.txtDoor12_03.Text);
                        m_ALDoors.Add(Door12Sub000001_01.txtDoor12_04.Text);
                        m_ALDoors.Add(Door12Sub000001_01.txtDoor12_05.Text);
                        m_ALDoors.Add(Door12Sub000001_01.txtDoor12_06.Text);
                        m_ALDoors.Add(Door12Sub000001_01.txtDoor12_07.Text);
                        m_ALDoors.Add(Door12Sub000001_01.txtDoor12_08.Text);
                        m_ALDoors.Add(Door12Sub000001_01.txtDoor12_09.Text);
                        break;
                    case 08:
                        m_ALDoors.Add(Door12Sub000001_01.txtDoor12_01.Text);
                        m_ALDoors.Add(Door12Sub000001_01.txtDoor12_02.Text);
                        m_ALDoors.Add(Door12Sub000001_01.txtDoor12_03.Text);
                        m_ALDoors.Add(Door12Sub000001_01.txtDoor12_04.Text);
                        m_ALDoors.Add(Door12Sub000001_01.txtDoor12_05.Text);
                        m_ALDoors.Add(Door12Sub000001_01.txtDoor12_06.Text);
                        m_ALDoors.Add(Door12Sub000001_01.txtDoor12_07.Text);
                        m_ALDoors.Add(Door12Sub000001_01.txtDoor12_08.Text);
                        break;
                    case 07:
                        m_ALDoors.Add(Door12Sub000001_01.txtDoor12_01.Text);
                        m_ALDoors.Add(Door12Sub000001_01.txtDoor12_02.Text);
                        m_ALDoors.Add(Door12Sub000001_01.txtDoor12_03.Text);
                        m_ALDoors.Add(Door12Sub000001_01.txtDoor12_04.Text);
                        m_ALDoors.Add(Door12Sub000001_01.txtDoor12_05.Text);
                        m_ALDoors.Add(Door12Sub000001_01.txtDoor12_06.Text);
                        m_ALDoors.Add(Door12Sub000001_01.txtDoor12_07.Text);
                        break;
                    case 06:
                        m_ALDoors.Add(Door12Sub000001_01.txtDoor12_01.Text);
                        m_ALDoors.Add(Door12Sub000001_01.txtDoor12_02.Text);
                        m_ALDoors.Add(Door12Sub000001_01.txtDoor12_03.Text);
                        m_ALDoors.Add(Door12Sub000001_01.txtDoor12_04.Text);
                        m_ALDoors.Add(Door12Sub000001_01.txtDoor12_05.Text);
                        m_ALDoors.Add(Door12Sub000001_01.txtDoor12_06.Text);
                        break;
                    case 05:
                        m_ALDoors.Add(Door12Sub000001_01.txtDoor12_01.Text);
                        m_ALDoors.Add(Door12Sub000001_01.txtDoor12_02.Text);
                        m_ALDoors.Add(Door12Sub000001_01.txtDoor12_03.Text);
                        m_ALDoors.Add(Door12Sub000001_01.txtDoor12_04.Text);
                        m_ALDoors.Add(Door12Sub000001_01.txtDoor12_05.Text);
                        break;
                    case 04:
                        m_ALDoors.Add(Door12Sub000001_01.txtDoor12_01.Text);
                        m_ALDoors.Add(Door12Sub000001_01.txtDoor12_02.Text);
                        m_ALDoors.Add(Door12Sub000001_01.txtDoor12_03.Text);
                        m_ALDoors.Add(Door12Sub000001_01.txtDoor12_04.Text);
                        break;
                    case 03:
                        m_ALDoors.Add(Door12Sub000001_01.txtDoor12_01.Text);
                        m_ALDoors.Add(Door12Sub000001_01.txtDoor12_02.Text);
                        m_ALDoors.Add(Door12Sub000001_01.txtDoor12_03.Text);
                        break;
                    case 02:
                        m_ALDoors.Add(Door12Sub000001_01.txtDoor12_01.Text);
                        m_ALDoors.Add(Door12Sub000001_01.txtDoor12_02.Text);
                        break;
                    case 01:
                        m_ALDoors.Add(Door12Sub000001_01.txtDoor12_01.Text);
                        break;
                }
            }
            //--
            //--
            //holiday
            m_ALSub000001Name.Clear();
            m_ALSub000001Date.Clear();
            m_ALSub000001State.Clear();
            for (int i = 0; i < dgvSub000001_01.Rows.Count; i++)
            {
                String date = dgvSub000001_01.Rows[i].Cells[1].Value.ToString();//抓取DataGridView欄位資料
                String name = dgvSub000001_01.Rows[i].Cells[2].Value.ToString();//抓取DataGridView欄位資料
                String state = dgvSub000001_01.Rows[i].Cells[3].Value.ToString();//抓取DataGridView欄位資料
                m_ALSub000001Name.Add(name);
                m_ALSub000001Date.Add(date);
                m_ALSub000001State.Add(state);
            }
            //--
            //add at 2017/10/05
            if (!blnSaveDB)
            {
                m_Sub000001ALData.Clear();

                m_Sub000001ALData.Add(txtSub000001_01.Text);//add at 2017/10/05
                m_Sub000001ALData.Add(txtSub000001_02.Text);//add at 2017/10/05
                m_Sub000001ALData.Add(labSub000001_09.Text);//add at 2017/10/05
                m_Sub000001ALData.Add(txtSub000001_03.Text);//add at 2017/10/05
                m_Sub000001ALData.Add(txtSub000001_04.Text);//add at 2017/10/05
                m_Sub000001ALData.Add(rdbSub000001_01.Checked.ToString());//add at 2017/10/05
                m_Sub000001ALData.Add(ckbSub000001_01.Checked.ToString());//add at 2017/10/05
                m_Sub000001ALData.Add(rdbSub000001_03.Checked.ToString());//add at 2017/10/05
                m_Sub000001ALData.Add(rdbSub000001_04.Checked.ToString());//add at 2017/10/05
                m_Sub000001ALData.Add(ckbSub000001_02.Checked.ToString());//add at 2017/10/05
                m_Sub000001ALData.Add(txtSub000001_19.Value + "");//add at 2017/10/05
                m_Sub000001ALData.Add(cmbSub000001_01.SelectedIndex + "");//add at 2017/10/05
                m_Sub000001ALData.Add(cmbSub000001_03.SelectedIndex + "");//add at 2017/10/05
                m_Sub000001ALData.Add(cmbSub000001_04.SelectedIndex + "");//SYDM 選擇元件 修改偵測

                for (int i = 0; i < m_ALSub000001Date.Count; i++)//add at 2017/10/05
                {
                    m_Sub000001ALData.Add(m_ALSub000001Date[i].ToString());
                }

                for (int j = 0; j < m_ALDoors.Count; j++)//add at 2017/10/05
                {
                    m_Sub000001ALData.Add(m_ALDoors[j].ToString());
                }

                return CheckUIVarNotChange(m_Sub000001ALInit, m_Sub000001ALData);
            }
            //--

            String StrSQL = "";
            if (m_intcontroller_sn == -1)//新增控制器 at 2017/06/28
            {
                bool blnunique = true;

                //--
                //新增時，判斷資料是否重複 at 2017/08/07
                if (Strconnetction_mode == "0")
                {
                    StrSQL = String.Format("SELECT controller_sn,connetction_address FROM controller_extend WHERE connetction_address='{1}';", Strcontroller_sn, Strconnetction_address);
                }
                else
                {
                    StrSQL = String.Format("SELECT controller_sn,connetction_address FROM controller_extend WHERE controller_sn={0};", Strcontroller_sn, Strconnetction_address);
                }
                MySqlDataReader Reader_Detect = MySQL.GetDataReader(StrSQL);
                while (Reader_Detect.Read())
                {
                    blnunique = false;
                    break;
                }
                Reader_Detect.Close();
                //--


                m_intcontroller_sn = 0;
                if (blnunique)
                {

                    //---
                    //修改 儲存控制器時支援 SYDM 選擇元件
                    if (m_changeSYCGMode)
                    {
                        if ((m_ALSYDM_ID.Count > 0) && (cmbSub000001_04.SelectedIndex == -1))
                        {
                            cmbSub000001_04.SelectedIndex = 0;
                        }
                    }
                    else
                    {
                        cmbSub000001_04.SelectedIndex = -1;
                    }

                    if (cmbSub000001_04.SelectedIndex == -1)
                    {
                        Strsydm_id = "0";
                    }
                    else
                    {
                        Strsydm_id = (String)m_ALSYDM_ID[cmbSub000001_04.SelectedIndex];
                    }
                    //---

                    //--
                    //controller
                    StrSQL = "";
                    StrSQL = String.Format("INSERT INTO controller (sn,model,name,alias,state,sydm_id) VALUES ({0},{1}, '{2}','{3}',{4},{5});", Strsn, Strmodel, Strname, Stralias, Strstate, Strsydm_id);//修改 儲存控制器時支援 SYDM 選擇元件
                    MySQL.InsertUpdateDelete(StrSQL);
                    //--

                    //--
                    //controll_extend
                    StrSQL = "";
                    StrSQL = String.Format("INSERT INTO controller_extend (controller_sn,connetction_address,connetction_enabled,connetction_mode,apb_enable,apb_mode,apb_group,apb_level_list,apb_reset_timestamp_list,ab_door_enabled,ab_door_level,ab_door_timeout_second,ab_door_reset_time_second,state,port,door_number,same_card_interval_time_second) VALUES ({0},'{1}',{2},{3},{4},{5},{6},'{7}','{8}',{9},{10},{11},{12},{13},{14},{15},{16});",
                                           Strcontroller_sn, Strconnetction_address, Strconnetction_enabled, Strconnetction_mode, Strapb_enable, Strapb_mode, Strapb_group, Strapb_level_list, Strapb_reset_timestamp_list, Strab_door_enabled, Strab_door_level, Strab_door_timeout_second, Strab_door_reset_time_second, Strstate, Strport, Strdoor_number, Strsame_card_interval_time_second);
                    MySQL.InsertUpdateDelete(StrSQL);
                    //--

                    //--
                    //door
                    StrSQL = "";
                    for (int i = 0; i < m_ALDoors.Count; i++)
                    {
                        String StrName = "";
                        StrName = m_ALDoors[i].ToString();
                        if(StrName=="")
                        {
                            StrName = String.Format("door_{0:00}({1})", (i + 1), Strcontroller_sn);
                        }
                        StrSQL += String.Format("INSERT INTO door (controller_id,name,controller_door_index,state) VALUES ({0},'{1}',{2},{3});", Strcontroller_sn, StrName, (i + 1), Strstate);//增加 controller_door_index 欄位 at 2017/08/04
                    }
                    MySQL.InsertUpdateDelete(StrSQL);
                    //--
                }
                else
                {
                    MessageBox.Show(Language.m_StrControlData2DBMsg00, butSub0000_02.Text, MessageBoxButtons.OK, MessageBoxIcon.Error);
                    blnAns = false;
                    return blnAns;
                }
            }
            else//更新控制器
            {
                //--
                //controller
                StrSQL = String.Format("UPDATE controller SET name = '{0}', alias = '{1}', state={3}  WHERE sn ={2};", Strname, Stralias, m_intcontroller_sn, Strstate);
                MySQL.InsertUpdateDelete(StrSQL);
                //--
                //--
                //controll_extend
                StrSQL = "";
                StrSQL = String.Format("UPDATE controller_extend SET connetction_address = '{0}', connetction_enabled = {1}, connetction_mode = {2}, apb_enable = {3}, apb_mode = {4}, apb_group = {5}, apb_level_list = '{6}', apb_reset_timestamp_list = '{7}', ab_door_enabled = {8}, ab_door_level = {9}, ab_door_timeout_second = {10}, ab_door_reset_time_second = {11},port={12},door_number={13}, state={15},same_card_interval_time_second={16} WHERE controller_sn={14};", Strconnetction_address, Strconnetction_enabled, Strconnetction_mode, Strapb_enable, Strapb_mode, Strapb_group, Strapb_level_list, Strapb_reset_timestamp_list, Strab_door_enabled, Strab_door_level, Strab_door_timeout_second, Strab_door_reset_time_second, Strport, Strdoor_number, m_intcontroller_sn, Strstate,Strsame_card_interval_time_second);
                MySQL.InsertUpdateDelete(StrSQL);
                //--

                //--
                //door
                StrSQL = "";
                for (int i = 0; i < m_ALDoors.Count; i++)
                {
                    String StrName = "";
                    StrName = m_ALDoors[i].ToString();
                    if (StrName == "")
                    {
                        StrName = String.Format("door_{0:00}({1})", (i + 1), Strcontroller_sn);
                    }
                    StrSQL += String.Format("UPDATE door SET name='{0}',state={2} WHERE id={1};", StrName, m_ALDoor_id[i].ToString(), Strstate);
                }
                MySQL.InsertUpdateDelete(StrSQL);
                //--
                m_intcontroller_sn = 0;
            }
            //--
            //holiday
            StrSQL = String.Format("DELETE FROM holiday WHERE controller_id = {0};", labSub000001_09.Text); //要把假日列表，依控制器獨立切開 at 2017/08/16 -- StrSQL = "DELETE FROM holiday WHERE controller_id = 0;";//刪除原本假日表
            MySQL.InsertUpdateDelete(StrSQL);
            StrSQL = "";
            for (int i = 0; i < m_ALSub000001Name.Count; i++)
            {
                String name = m_ALSub000001Name[i].ToString();
                String date = "1981-" + m_ALSub000001Date[i].ToString().Replace('/', '-');
                String status = "0";
                if (m_ALSub000001State[i].ToString().ToLower() == "enable")
                {
                    status = "1";
                }
                StrSQL += String.Format("INSERT INTO holiday (name,date,status,controller_id,state) VALUES ('{0}','{1}',{2},{3},1);", name, date, status, labSub000001_09.Text); //要把假日列表，依控制器獨立切開 at 2017/08/16 -- StrSQL += String.Format("INSERT INTO holiday (name,date,status,controller_id,state) VALUES ('{0}','{1}',{2},0,1);", name, date, status);
            }
            MySQL.InsertUpdateDelete(StrSQL);
            //--
            return blnAns;
        }
        private void butSub000001_12_Click(object sender, EventArgs e)//控制器儲存設定(新增/更新)
        {
            //--
            //測試用產生空的Add Controller JSON at 2017/08/08
            Add_Controller Add_Controller1 = new Add_Controller();
            Add_Controller1.apb_and_ab_door = new AC_ApbAndAbDoor();
            Add_Controller1.connection = new AC_Connection();
            String Data = parseJSON.composeJSON_Add_Controller(Add_Controller1);
            //--

            if (ControlData2DB())//把設定控制器UI資料寫入DB中 at 2017/08/03
            {
                //---
                //控制器UI多選編輯實作 ~ 撰寫對應鍵盤事件

                //Leave_function();
                if (m_ALControllerObj.Count <= 1)
                {
                    Leave_function();
                }

                //---控制器UI多選編輯實作 ~ 撰寫對應鍵盤事件
            }
        }
        private void butSub000001_14_Click(object sender, EventArgs e)//設置控制器頁面，離開按鈕事件
        {
            if ((m_intcontroller_sn == 0) || (ControlData2DB(false) == true))//add at 2017/10/05 if (m_intcontroller_sn == 0)
            {
                labSub000001_03.ForeColor = Color.Black;//add at 2017/10/05
                labSub000001_01.ForeColor = Color.Black;//add at 2017/10/05
                labSub000001_04.ForeColor = Color.Black;//add at 2017/10/05	
                labSub000001_20.ForeColor = Color.Black;//SYCG模式下新增控制器一定要有SYDM的防呆機制
                Leave_function();
            }
            else
            {
                DialogResult myResult = MessageBox.Show(Language.m_StrControllerMsg00, butSub000001_14.Text, MessageBoxButtons.YesNo, MessageBoxIcon.Question);
                if (myResult == DialogResult.Yes)
                {
                    Leave_function();
                }
            }
        }
        //Sub000001_end
        //Sub020000_start
        private void butSub020000_09_Click(object sender, EventArgs e)
        {
            m_TPOld = m_tabMain.SelectedTab;//--2017/02/22 製作返回按鈕功能
            TabPage_Push();//m_StackTPOld.Push(m_TPOld);//--2017/02/22 製作返回按鈕功能

            m_tabSub0201.Parent = m_tabMain;
            m_tabMain.SelectedTab = m_tabSub0201;
        }
        //Sub020000_end
        //Sub000301_start
        private void dgvSub000301_01_DoubleClick(object sender, EventArgs e)//at 2017/09/15
        {
            butSub000301_14.PerformClick();
        }

        private void butSub000301_15_Click(object sender, EventArgs e)//新增
        {
            clear_APBTmpData();//清除新增但尚未儲存的暫存資料
            m_intDB2LeftSub000301_id = -10;
            initLeftSub000301UI();

            //---
            //新增所有群組時都預設填入名稱
            //txtSub000301_01.Focus();//2017/03/30 頁面切換後，指定該頁面特定元件取的焦點(Focus)
            txtSub000301_01.Text = "group_apb_" + DateTime.Now.ToString("yyyyMMddHHmmss");
            //---新增所有群組時都預設填入名稱

            m_Sub000301ALInit.Clear();//add at 2017/10/06
            m_Sub000301ALInit.Add(txtSub000301_01.Text);//add at 2017/10/06
            m_Sub000301ALInit.Add(rdbSub000301_01.Checked.ToString());//add at 2017/10/06
            m_Sub000301ALInit.Add(rdbSub000301_02.Checked.ToString());//add at 2017/10/06
            m_Sub000301ALInit.Add(steSub000301_01.blnEnable.ToString());//add at 2017/10/06
            m_Sub000301ALInit.Add(steSub000301_01.StrValue);//add at 2017/10/06
            m_Sub000301ALInit.Add(steSub000301_01.ckb_001.Checked.ToString());//add at 2017/10/06
            m_Sub000301ALInit.Add(steSub000301_02.blnEnable.ToString());//add at 2017/10/06
            m_Sub000301ALInit.Add(steSub000301_02.StrValue);//add at 2017/10/06
            m_Sub000301ALInit.Add(steSub000301_02.ckb_001.Checked.ToString());//add at 2017/10/06
            m_Sub000301ALInit.Add(steSub000301_03.blnEnable.ToString());//add at 2017/10/06
            m_Sub000301ALInit.Add(steSub000301_03.StrValue);//add at 2017/10/06
            m_Sub000301ALInit.Add(steSub000301_03.ckb_001.Checked.ToString());//add at 2017/10/06
            m_Sub000301ALInit.Add(steSub000301_04.blnEnable.ToString());//add at 2017/10/06
            m_Sub000301ALInit.Add(steSub000301_04.StrValue);//add at 2017/10/06
            m_Sub000301ALInit.Add(steSub000301_04.ckb_001.Checked.ToString());//add at 2017/10/06
            m_Sub000301ALInit.Add(steSub000301_05.blnEnable.ToString());//add at 2017/10/06
            m_Sub000301ALInit.Add(steSub000301_05.StrValue);
            m_Sub000301ALInit.Add(steSub000301_05.ckb_001.Checked.ToString());//add at 2017/10/06
            m_Sub000301ALInit.Add(steSub000301_06.blnEnable.ToString());//add at 2017/10/06
            m_Sub000301ALInit.Add(steSub000301_06.StrValue);
            m_Sub000301ALInit.Add(steSub000301_06.ckb_001.Checked.ToString());//add at 2017/10/06
            m_Sub000301ALInit.Add(steSub000301_07.blnEnable.ToString());//add at 2017/10/06
            m_Sub000301ALInit.Add(steSub000301_07.StrValue);
            m_Sub000301ALInit.Add(steSub000301_07.ckb_001.Checked.ToString());//add at 2017/10/06
            m_Sub000301ALInit.Add(steSub000301_08.blnEnable.ToString());//add at 2017/10/06
            m_Sub000301ALInit.Add(steSub000301_08.StrValue);//add at 2017/10/06
            m_Sub000301ALInit.Add(steSub000301_08.ckb_001.Checked.ToString());//add at 2017/10/06

        }

        private void butSub000301_14_Click(object sender, EventArgs e)//編修
        {
            clear_APBTmpData();//清除新增但尚未儲存的暫存資料
            if (m_intdgvSub000301_01_id < 0)
            {
                return;
            }

            DB2LeftSub000301UI(m_intdgvSub000301_01_id);

            txtSub000301_01.Focus();//--2017/03/30 頁面切換後，指定該頁面特定元件取的焦點(Focus)

            m_Sub000301ALInit.Clear();//add at 2017/10/06
            m_Sub000301ALInit.Add(txtSub000301_01.Text);//add at 2017/10/06
            m_Sub000301ALInit.Add(rdbSub000301_01.Checked.ToString());//add at 2017/10/06
            m_Sub000301ALInit.Add(rdbSub000301_02.Checked.ToString());//add at 2017/10/06
            m_Sub000301ALInit.Add(steSub000301_01.blnEnable.ToString());//add at 2017/10/06
            m_Sub000301ALInit.Add(steSub000301_01.StrValue);//add at 2017/10/06
            m_Sub000301ALInit.Add(steSub000301_01.ckb_001.Checked.ToString());//add at 2017/10/06
            m_Sub000301ALInit.Add(steSub000301_02.blnEnable.ToString());//add at 2017/10/06
            m_Sub000301ALInit.Add(steSub000301_02.StrValue);//add at 2017/10/06
            m_Sub000301ALInit.Add(steSub000301_02.ckb_001.Checked.ToString());//add at 2017/10/06
            m_Sub000301ALInit.Add(steSub000301_03.blnEnable.ToString());//add at 2017/10/06
            m_Sub000301ALInit.Add(steSub000301_03.StrValue);//add at 2017/10/06
            m_Sub000301ALInit.Add(steSub000301_03.ckb_001.Checked.ToString());//add at 2017/10/06
            m_Sub000301ALInit.Add(steSub000301_04.blnEnable.ToString());//add at 2017/10/06
            m_Sub000301ALInit.Add(steSub000301_04.StrValue);//add at 2017/10/06
            m_Sub000301ALInit.Add(steSub000301_04.ckb_001.Checked.ToString());//add at 2017/10/06
            m_Sub000301ALInit.Add(steSub000301_05.blnEnable.ToString());//add at 2017/10/06
            m_Sub000301ALInit.Add(steSub000301_05.StrValue);
            m_Sub000301ALInit.Add(steSub000301_05.ckb_001.Checked.ToString());//add at 2017/10/06
            m_Sub000301ALInit.Add(steSub000301_06.blnEnable.ToString());//add at 2017/10/06
            m_Sub000301ALInit.Add(steSub000301_06.StrValue);
            m_Sub000301ALInit.Add(steSub000301_06.ckb_001.Checked.ToString());//add at 2017/10/06
            m_Sub000301ALInit.Add(steSub000301_07.blnEnable.ToString());//add at 2017/10/06
            m_Sub000301ALInit.Add(steSub000301_07.StrValue);
            m_Sub000301ALInit.Add(steSub000301_07.ckb_001.Checked.ToString());//add at 2017/10/06
            m_Sub000301ALInit.Add(steSub000301_08.blnEnable.ToString());//add at 2017/10/06
            m_Sub000301ALInit.Add(steSub000301_08.StrValue);//add at 2017/10/06
            m_Sub000301ALInit.Add(steSub000301_08.ckb_001.Checked.ToString());//add at 2017/10/06

        }

        private void butSub000301_23_Click(object sender, EventArgs e)//離開
        {
            m_Sub000301ALData.Clear();//add at 2017/10/06
            m_Sub000301ALData.Add(txtSub000301_01.Text);//add at 2017/10/06
            m_Sub000301ALData.Add(rdbSub000301_01.Checked.ToString());//add at 2017/10/06
            m_Sub000301ALData.Add(rdbSub000301_02.Checked.ToString());//add at 2017/10/06
            m_Sub000301ALData.Add(steSub000301_01.blnEnable.ToString());//add at 2017/10/06
            m_Sub000301ALData.Add(steSub000301_01.StrValue);//add at 2017/10/06
            m_Sub000301ALData.Add(steSub000301_01.ckb_001.Checked.ToString());//add at 2017/10/06
            m_Sub000301ALData.Add(steSub000301_02.blnEnable.ToString());//add at 2017/10/06
            m_Sub000301ALData.Add(steSub000301_02.StrValue);//add at 2017/10/06
            m_Sub000301ALData.Add(steSub000301_02.ckb_001.Checked.ToString());//add at 2017/10/06
            m_Sub000301ALData.Add(steSub000301_03.blnEnable.ToString());//add at 2017/10/06
            m_Sub000301ALData.Add(steSub000301_03.StrValue);//add at 2017/10/06
            m_Sub000301ALData.Add(steSub000301_03.ckb_001.Checked.ToString());//add at 2017/10/06
            m_Sub000301ALData.Add(steSub000301_04.blnEnable.ToString());//add at 2017/10/06
            m_Sub000301ALData.Add(steSub000301_04.StrValue);//add at 2017/10/06
            m_Sub000301ALData.Add(steSub000301_04.ckb_001.Checked.ToString());//add at 2017/10/06
            m_Sub000301ALData.Add(steSub000301_05.blnEnable.ToString());//add at 2017/10/06
            m_Sub000301ALData.Add(steSub000301_05.StrValue);
            m_Sub000301ALData.Add(steSub000301_05.ckb_001.Checked.ToString());//add at 2017/10/06
            m_Sub000301ALData.Add(steSub000301_06.blnEnable.ToString());//add at 2017/10/06
            m_Sub000301ALData.Add(steSub000301_06.StrValue);
            m_Sub000301ALData.Add(steSub000301_06.ckb_001.Checked.ToString());//add at 2017/10/06
            m_Sub000301ALData.Add(steSub000301_07.blnEnable.ToString());//add at 2017/10/06
            m_Sub000301ALData.Add(steSub000301_07.StrValue);
            m_Sub000301ALData.Add(steSub000301_07.ckb_001.Checked.ToString());//add at 2017/10/06
            m_Sub000301ALData.Add(steSub000301_08.blnEnable.ToString());//add at 2017/10/06
            m_Sub000301ALData.Add(steSub000301_08.StrValue);//add at 2017/10/06
            m_Sub000301ALData.Add(steSub000301_08.ckb_001.Checked.ToString());//add at 2017/10/06	

            if ((m_intDB2LeftSub000301_id == -1) || CheckUIVarNotChange(m_Sub000301ALInit, m_Sub000301ALData))
            {
                initSub0003UI();
                Leave_function();
            }
            else
            {
                DialogResult myResult = MessageBox.Show(Language.m_StrControllerMsg00, butSub000301_03.Text.Trim() + "/" + butSub000301_02.Text.Trim(), MessageBoxButtons.YesNo, MessageBoxIcon.Question);
                if (myResult == DialogResult.Yes)
                {
                    initSub0003UI();
                    Leave_function();
                }
            }
        }

        private void butSub000301_19_Click(object sender, EventArgs e)//A.P.B全選
        {
            /*
            for (int i = 0; i < dgvSub000301_01.Rows.Count; i++)
            {
                dgvSub000301_01.Rows[i].Cells[0].Value = true;
                dgvSub000301_01.Rows[i].Selected = true;
            }
            */
            dgvSub000301_01.SelectAll();//把所有列表元件的全選/取消全選改成SelectAll()/ClearSelection()
        }

        private void butSub000301_20_Click(object sender, EventArgs e)//A.P.B取消全選
        {
            /*
            for (int i = 0; i < dgvSub000301_01.Rows.Count; i++)
            {
                dgvSub000301_01.Rows[i].Cells[0].Value = false;
                dgvSub000301_01.Rows[i].Selected = false;
            }
            */
            dgvSub000301_01.ClearSelection();//把所有列表元件的全選/取消全選改成SelectAll()/ClearSelection()
        }
        private void butSub000301_22_Click(object sender, EventArgs e)//A.P.B搜尋
        {
            initdgvSub000301_01();
            ArrayList AL01 = new ArrayList();
            ArrayList AL02 = new ArrayList();
            ArrayList AL03 = new ArrayList();
            ArrayList AL04 = new ArrayList();
            ArrayList AL05 = new ArrayList();
            AL01.Clear();
            AL02.Clear();
            AL03.Clear();
            AL04.Clear();
            AL05.Clear();


            if (txtSub000301_04.Text != "")
            {
                for (int i = 0; i < dgvSub000301_01.Rows.Count; i++)//取的現行UI上控制器列表所有資料
                {
                    AL01.Add(dgvSub000301_01.Rows[i].Cells[1].Value.ToString());
                    AL02.Add(dgvSub000301_01.Rows[i].Cells[2].Value.ToString());
                    AL03.Add(dgvSub000301_01.Rows[i].Cells[3].Value.ToString());
                    AL04.Add(dgvSub000301_01.Rows[i].Cells[4].Value.ToString());
                    AL05.Add(dgvSub000301_01.Rows[i].Cells[5].Value.ToString());
                }
                try
                {
                    //--
                    //dgvSub000301_01.ReadOnly = true;//唯讀 不可更改
                    dgvSub000301_01.RowHeadersVisible = false;//DataGridView 最前面指示選取列所在位置的箭頭欄位
                    dgvSub000301_01.Rows[0].Selected = false;//取消DataGridView的默認選取(選中)Cell 使其不反藍
                    dgvSub000301_01.AllowUserToAddRows = false;//是否允許使用者新增資料
                    dgvSub000301_01.AllowUserToDeleteRows = false;//是否允許使用者刪除資料
                    dgvSub000301_01.AllowUserToOrderColumns = false;//是否允許使用者調整欄位位置
                    //所有表格欄位寬度全部變成可調 dgvSub000301_01.AllowUserToResizeColumns = false;//是否允許使用者改變欄寬
                    dgvSub000301_01.AllowUserToResizeRows = false;//是否允許使用者改變行高
                    dgvSub000301_01.Columns[1].ReadOnly = true;//單一欄位禁止編輯
                    dgvSub000301_01.Columns[2].ReadOnly = true;//單一欄位禁止編輯
                    dgvSub000301_01.Columns[3].ReadOnly = true;//單一欄位禁止編輯
                    dgvSub000301_01.Columns[4].ReadOnly = true;//單一欄位禁止編輯
                    dgvSub000301_01.Columns[5].ReadOnly = true;//單一欄位禁止編輯
                    dgvSub000301_01.AllowUserToAddRows = false;//刪除空白列
                    dgvSub000301_01.SelectionMode = System.Windows.Forms.DataGridViewSelectionMode.FullRowSelect;//整列選取
                    //--

                    do
                    {
                        for (int i = 0; i < dgvSub000301_01.Rows.Count; i++)
                        {
                            DataGridViewRow r1 = this.dgvSub000301_01.Rows[i];//取得DataGridView整列資料
                            this.dgvSub000301_01.Rows.Remove(r1);//DataGridView刪除整列
                        }
                    } while (dgvSub000301_01.Rows.Count > 0);

                }
                catch
                {
                }
                String StrSearch = txtSub000301_04.Text;
                for (int i = 0; i < AL01.Count; i++)
                {
                    //AL01[i].ToString()->DB index 本來就被隱藏 所以不用在搜尋欄位內
                    if ((AL02[i].ToString().IndexOf(StrSearch) > -1) || (AL03[i].ToString().IndexOf(StrSearch) > -1) || (AL04[i].ToString().IndexOf(StrSearch) > -1) || (AL05[i].ToString().IndexOf(StrSearch) > -1))
                    {
                        this.dgvSub000301_01.Rows.Add(false, AL01[i].ToString(), AL02[i].ToString(), AL03[i].ToString(), AL04[i].ToString(), AL05[i].ToString());
                    }
                }
            }
        }

        private void butSub000301_07_Click(object sender, EventArgs e)//全選
        {
            tvmSub000301_01._bPreventCheckEvent = false;//克服元件在點選展開後就無法實現設定子節點連動改變父節點的BUG at 2017/07/12 21:06
            for (int i = 0; i < tvmSub000301_01.Nodes.Count; i++)
            {
                tvmSub000301_01.Nodes[i].Checked = true;
            }
        }

        private void butSub000301_08_Click(object sender, EventArgs e)//取消全選
        {
            tvmSub000301_01._bPreventCheckEvent = false;//克服元件在點選展開後就無法實現設定子節點連動改變父節點的BUG at 2017/07/12 21:06
            for (int i = 0; i < tvmSub000301_01.Nodes.Count; i++)
            {
                tvmSub000301_01.Nodes[i].Checked = false;
            }
        }

        private void butSub000301_09_Click(object sender, EventArgs e)//加入門區
        {
            int inttmp=-10;
            String SQL = "";
            Sub000301_getTreeView(tvmSub000301_01);
            if (m_intDB2LeftSub000301_id > 0)//!=-1
            {
                inttmp = m_intDB2LeftSub000301_id;
            }
            for(int i=0;i<m_ALdoor_apb_select.Count;i++)
            {
                SQL = "";
                SQL =String.Format("INSERT INTO apb_door (apb_group_id, door_id, state) VALUES ({0},{1},1);",inttmp,m_ALdoor_apb_select[i].ToString());//新增右側列表項目
                MySQL.InsertUpdateDelete(SQL);
                SQL = "";
                SQL = String.Format("UPDATE door_extend SET apb_group_id={0},apb_used=1,state=1,apb_level={2} WHERE door_id={1};", inttmp, m_ALdoor_apb_select[i].ToString(), txtSub000301_05.Value);//更新對應選擇的門區
                MySQL.InsertUpdateDelete(SQL);
                SQL = "";
                SQL = String.Format("UPDATE door_extend SET apb_group_id={0},state=1 WHERE door_id IN ( SELECT id FROM door WHERE controller_id IN (SELECT controller_id AS sn FROM door WHERE id={1}) );", inttmp, m_ALdoor_apb_select[i].ToString());//更新相同控制器下所有門區
                MySQL.InsertUpdateDelete(SQL);
            }

            //--
            if (rdbSub000301_01.Checked == true)
            {
                if (m_intDB2LeftSub000301_id < 0)//新增~APB門區模式 ==-1
                {
                    initvmSub000301_01(0, m_intDB2LeftSub000301_id);
                }
                else//編修~APB門區模式 
                {
                    initvmSub000301_01(2, m_intDB2LeftSub000301_id);
                }
            }
            else if (rdbSub000301_02.Checked == true)
            {

                if (m_intDB2LeftSub000301_id < 0)//新增~APB次數模式 ==-1
                {
                    initvmSub000301_01(1, m_intDB2LeftSub000301_id);
                }
                else//編修~APB次數模式
                {
                    initvmSub000301_01(3, m_intDB2LeftSub000301_id);
                }
            }
            //--
            initvmSub000301_02(inttmp);
        }

        private void butSub000301_11_Click(object sender, EventArgs e)//移出門區
        {
            int inttmp = -10;
            String SQL = "";
            if (m_intDB2LeftSub000301_id > 0)//!=-1
            {
                inttmp = m_intDB2LeftSub000301_id;
            }
            for (int i = 0; i < tvmSub000301_02.Nodes.Count; i++)
            {
                if (tvmSub000301_02.Nodes[i].Checked == true)
                {
                    int door_id = ((Tree_Node)tvmSub000301_02.Nodes[i]).m_id;
                    SQL = "";
                    SQL = String.Format("DELETE FROM apb_door WHERE door_id={0} AND apb_group_id={1};",door_id,inttmp);//刪除右側列表項目
                    MySQL.InsertUpdateDelete(SQL);
                    SQL = "";
                    SQL = String.Format("UPDATE door_extend SET apb_used=0,state=1,apb_level=1 WHERE door_id={0};", door_id);//設定該門區屬性為沒有APB
                    MySQL.InsertUpdateDelete(SQL);
                    SQL = "";
                    int num = 0;
                    SQL = String.Format("SELECT count(door_id) AS num FROM door_extend WHERE door_id IN (SELECT id FROM  door WHERE controller_id IN (SELECT controller_id AS sn FROM door WHERE id={0})) AND apb_used=1;",door_id);
                    MySqlDataReader ReaderData = MySQL.GetDataReader(SQL);
                    while (ReaderData.Read())
                    {
                        num = Convert.ToInt32(ReaderData["num"].ToString());
                        break;
                    }
                    ReaderData.Close();
                    if(num==0)
                    {
                        SQL = "";
                        SQL = String.Format("UPDATE door_extend SET apb_group_id=-1,state=1 WHERE door_id IN ( SELECT id FROM door WHERE controller_id IN (SELECT controller_id AS sn FROM door WHERE id={0}) );", door_id);
                        MySQL.InsertUpdateDelete(SQL);
                    }
                }
            }

            //--
            if (rdbSub000301_01.Checked == true)
            {
                if (m_intDB2LeftSub000301_id < 0)//新增~APB門區模式 ==-1
                {
                    initvmSub000301_01(0, m_intDB2LeftSub000301_id);
                }
                else//編修~APB門區模式 
                {
                    initvmSub000301_01(2, m_intDB2LeftSub000301_id);
                }
            }
            else if (rdbSub000301_02.Checked == true)
            {

                if (m_intDB2LeftSub000301_id < 0)//新增~APB次數模式 ==-1
                {
                    initvmSub000301_01(1, m_intDB2LeftSub000301_id);
                }
                else//編修~APB次數模式
                {
                    initvmSub000301_01(3, m_intDB2LeftSub000301_id);
                }
            }
            //--
            initvmSub000301_02(inttmp);
        }

        private void butSub000301_12_Click(object sender, EventArgs e)//全選
        {
            tvmSub000301_02._bPreventCheckEvent = false;//克服元件在點選展開後就無法實現設定子節點連動改變父節點的BUG at 2017/07/12 21:06
            for (int i = 0; i < tvmSub000301_02.Nodes.Count; i++)
            {
                tvmSub000301_02.Nodes[i].Checked = true;
            }
        }

        private void butSub000301_13_Click(object sender, EventArgs e)//取消全選
        {
            tvmSub000301_02._bPreventCheckEvent = false;//克服元件在點選展開後就無法實現設定子節點連動改變父節點的BUG at 2017/07/12 21:06
            for (int i = 0; i < tvmSub000301_02.Nodes.Count; i++)
            {
                tvmSub000301_02.Nodes[i].Checked = false;
            }
        }
        public int AddAPB()
        {
            int intAns = -1;
            String SQL;
            String Strname, Strmode, Strdate;//apb_group
            String Strid, Strreset_time_1, Strreset_time_2, Strreset_time_3, Strreset_time_4, Strreset_time_5, Strreset_time_6, Strreset_time_7, Strreset_time_8;//apb_group_extend
            //--
            //apb_group
            Strname = txtSub000301_01.Text;
            Strmode = "0";
            if (rdbSub000301_01.Checked)
            {
                Strmode = "1";
            }
            else if (rdbSub000301_02.Checked)
            {
                Strmode = "2";
            }
            Strdate = DateTime.Now.ToString("yyyy-MM-dd");
            SQL = "";
            SQL = String.Format("INSERT INTO apb_group (name,mode,status,date,state) VALUES ('{0}',{1},1,'{2}',1);", Strname, Strmode, Strdate);
            MySQL.InsertUpdateDelete(SQL);
            //--
            //--
            //apb_group_extend
            Strid = "-1";
            Strreset_time_1 = "00:00";
            Strreset_time_2 = "00:00";
            Strreset_time_3 = "00:00";
            Strreset_time_4 = "00:00";
            Strreset_time_5 = "00:00";
            Strreset_time_6 = "00:00";
            Strreset_time_7 = "00:00";
            Strreset_time_8 = "00:00";

            SQL = "";
            SQL = String.Format("SELECT id FROM apb_group WHERE name='{0}' ORDER BY id DESC;", Strname);
            MySqlDataReader DataReader = MySQL.GetDataReader(SQL);
            while (DataReader.Read())
            {
                Strid = DataReader["id"].ToString();
                intAns = Convert.ToInt32(Strid);
                break;
            }
            DataReader.Close();

            Strreset_time_1 = steSub000301_01.StrValue + "-" + ((steSub000301_01.ckb_001.Checked) ? "1" : "0");//為了支援時間旁邊的打勾元件 at 2017/08/18
            Strreset_time_2 = steSub000301_02.StrValue + "-" + ((steSub000301_02.ckb_001.Checked) ? "1" : "0");//為了支援時間旁邊的打勾元件 at 2017/08/18
            Strreset_time_3 = steSub000301_03.StrValue + "-" + ((steSub000301_03.ckb_001.Checked) ? "1" : "0");//為了支援時間旁邊的打勾元件 at 2017/08/18
            Strreset_time_4 = steSub000301_04.StrValue + "-" + ((steSub000301_04.ckb_001.Checked) ? "1" : "0");//為了支援時間旁邊的打勾元件 at 2017/08/18
            Strreset_time_5 = steSub000301_05.StrValue + "-" + ((steSub000301_05.ckb_001.Checked) ? "1" : "0");//為了支援時間旁邊的打勾元件 at 2017/08/18
            Strreset_time_6 = steSub000301_06.StrValue + "-" + ((steSub000301_06.ckb_001.Checked) ? "1" : "0");//為了支援時間旁邊的打勾元件 at 2017/08/18
            Strreset_time_7 = steSub000301_07.StrValue + "-" + ((steSub000301_07.ckb_001.Checked) ? "1" : "0");//為了支援時間旁邊的打勾元件 at 2017/08/18
            Strreset_time_8 = steSub000301_08.StrValue + "-" + ((steSub000301_08.ckb_001.Checked) ? "1" : "0");//為了支援時間旁邊的打勾元件 at 2017/08/18

            SQL = "";
            SQL = String.Format("INSERT INTO apb_group_extend (apb_group_id,reset_time_1,reset_time_2,reset_time_3,reset_time_4,reset_time_5,reset_time_6,reset_time_7,reset_time_8,state) VALUES ({0},'{1}','{2}','{3}','{4}','{5}','{6}','{7}','{8}',1);", Strid, Strreset_time_1, Strreset_time_2, Strreset_time_3, Strreset_time_4, Strreset_time_5, Strreset_time_6, Strreset_time_7, Strreset_time_8);
            MySQL.InsertUpdateDelete(SQL);

            SQL = "";
            SQL = String.Format("UPDATE apb_door SET apb_group_id={0} WHERE apb_group_id=-10;", Strid);
            MySQL.InsertUpdateDelete(SQL);

            SQL = "";
            SQL = String.Format("UPDATE door_extend SET apb_group_id={0} WHERE apb_group_id=-10;", Strid);
            MySQL.InsertUpdateDelete(SQL);

            //--

            initLeftSub000301UI();
            initdgvSub000301_01();//刷新List

            //--
            m_intDB2LeftSub000301_id = -1;
            initLeftSub000301UI();
            //--

            initSub0003UI();

            return intAns;
        }

        private void butSub000301_03_Click(object sender, EventArgs e)//新增A.P.B
        {
            AddAPB();
            Leave_function();
        }

        public void SaveAPB()
        {
            String SQL;
            String Strname;//apb_group
            String Strreset_time_1, Strreset_time_2, Strreset_time_3, Strreset_time_4, Strreset_time_5, Strreset_time_6, Strreset_time_7, Strreset_time_8;//apb_group_extend
            //--
            //apb_group
            Strname = "";
            Strname = txtSub000301_01.Text;
            SQL = "";
            SQL = String.Format("UPDATE apb_group SET name='{0}',state=1 WHERE id={1};", Strname, m_intDB2LeftSub000301_id);
            MySQL.InsertUpdateDelete(SQL);
            //--
            //--
            //apb_group_extend
            Strreset_time_1 = steSub000301_01.StrValue + "-" + ((steSub000301_01.ckb_001.Checked) ? "1" : "0");//為了支援時間旁邊的打勾元件 at 2017/08/18
            Strreset_time_2 = steSub000301_02.StrValue + "-" + ((steSub000301_02.ckb_001.Checked) ? "1" : "0");//為了支援時間旁邊的打勾元件 at 2017/08/18
            Strreset_time_3 = steSub000301_03.StrValue + "-" + ((steSub000301_03.ckb_001.Checked) ? "1" : "0");//為了支援時間旁邊的打勾元件 at 2017/08/18
            Strreset_time_4 = steSub000301_04.StrValue + "-" + ((steSub000301_04.ckb_001.Checked) ? "1" : "0");//為了支援時間旁邊的打勾元件 at 2017/08/18
            Strreset_time_5 = steSub000301_05.StrValue + "-" + ((steSub000301_05.ckb_001.Checked) ? "1" : "0");//為了支援時間旁邊的打勾元件 at 2017/08/18
            Strreset_time_6 = steSub000301_06.StrValue + "-" + ((steSub000301_06.ckb_001.Checked) ? "1" : "0");//為了支援時間旁邊的打勾元件 at 2017/08/18
            Strreset_time_7 = steSub000301_07.StrValue + "-" + ((steSub000301_07.ckb_001.Checked) ? "1" : "0");//為了支援時間旁邊的打勾元件 at 2017/08/18
            Strreset_time_8 = steSub000301_08.StrValue + "-" + ((steSub000301_08.ckb_001.Checked) ? "1" : "0");//為了支援時間旁邊的打勾元件 at 2017/08/18

            SQL = "";
            SQL = String.Format("UPDATE apb_group_extend SET reset_time_1='{0}',reset_time_2='{1}',reset_time_3='{2}',reset_time_4='{3}',reset_time_5='{4}',reset_time_6='{5}',reset_time_7='{6}',reset_time_8='{7}',state=1 WHERE apb_group_id={8};", Strreset_time_1, Strreset_time_2, Strreset_time_3, Strreset_time_4, Strreset_time_5, Strreset_time_6, Strreset_time_7, Strreset_time_8, m_intDB2LeftSub000301_id);
            MySQL.InsertUpdateDelete(SQL);
            //--

            initLeftSub000301UI();
            initdgvSub000301_01();//刷新List

            //--
            m_intDB2LeftSub000301_id = -1;
            initLeftSub000301UI();
            //--

            initSub0003UI();
        }

        private void butSub000301_02_Click(object sender, EventArgs e)//儲存A.P.B
        {
            SaveAPB();
            Leave_function();
        }

        public CAAD_Controller DB2CAAD_Controller(int intsy_dm_Controller_id, String controller_sn)//Set Controller A.P.B & A/B
        {
            String SQL = "";
            CAAD_Controller CAAD_data = new CAAD_Controller();
            CAAD_data.apb_and_ab_door = new CAAD_ApbAndAbDoor();
            CAAD_data.apb_and_ab_door.apb_level_list = new List<int>();
            CAAD_data.apb_and_ab_door.apb_reset_timestamp_list = new List<int>();
            CAAD_data.apb_and_ab_door.apb_reset_mode_list = new List<int>();//為了支援時間旁邊的打勾元件 at 2017/08/18
            CAAD_data.identifier = intsy_dm_Controller_id;

            //CAAD_data.apb_and_ab_door.ab_door_enabled = SQL_data1;

            //CAAD_data.apb_and_ab_door.ab_door_level = SQL_data2;
            CAAD_data.apb_and_ab_door.apb_mode = 0;
            CAAD_data.apb_and_ab_door.ab_door_timeout_second = 30;//預設初始值
            CAAD_data.apb_and_ab_door.ab_door_reset_time_second = 60;//預設初始值
            int intdoor_number = 0;
            SQL = String.Format("SELECT ab_door_enabled,ab_door_level,apb_mode,ab_door_timeout_second,ab_door_reset_time_second,door_number FROM controller_extend WHERE controller_sn={0};", controller_sn);
            MySqlDataReader Reader_Data = MySQL.GetDataReader(SQL);
            while (Reader_Data.Read())
            {
                int tmp1 = 0, tmp2 = 0;

                CAAD_data.apb_and_ab_door.ab_door_enabled = Convert.ToInt32(Reader_Data["ab_door_enabled"].ToString());
                CAAD_data.apb_and_ab_door.ab_door_level = Convert.ToInt32(Reader_Data["ab_door_level"].ToString());
                CAAD_data.apb_and_ab_door.apb_mode = Convert.ToInt32(Reader_Data["apb_mode"].ToString());

                tmp1 = Convert.ToInt32(Reader_Data["ab_door_timeout_second"].ToString());
                tmp2 = Convert.ToInt32(Reader_Data["ab_door_reset_time_second"].ToString());
                intdoor_number = Convert.ToInt32(Reader_Data["door_number"].ToString());
                if ((tmp1 > 0) && (tmp2 > 0))
                {
                    CAAD_data.apb_and_ab_door.ab_door_timeout_second = tmp1;//從SQL拿
                    CAAD_data.apb_and_ab_door.ab_door_reset_time_second = tmp2;//從SQL拿
                }
                break;
            }
            Reader_Data.Close();

            if (CAAD_data.apb_and_ab_door.apb_mode > 0)
            {
                CAAD_data.apb_and_ab_door.apb_group = 0;//SQL  
                CAAD_data.apb_and_ab_door.apb_enabled = 1;
                SQL = String.Format("SELECT d_e.apb_group_id AS apb_group_id FROM door_extend AS d_e,door AS d,controller AS c WHERE (d_e.door_id=d.id) AND (d.controller_id=c.sn) AND c.sn={0};", controller_sn);
                MySqlDataReader Reader_Data1 = MySQL.GetDataReader(SQL);
                while (Reader_Data1.Read())
                {
                    int tmp = Convert.ToInt32(Reader_Data1["apb_group_id"].ToString());
                    if (tmp > 0)
                    {
                        CAAD_data.apb_and_ab_door.apb_group = tmp;
                    }
                    break;
                }
                Reader_Data1.Close();
            }
            else
            {
                CAAD_data.apb_and_ab_door.apb_enabled = 0;
                CAAD_data.apb_and_ab_door.apb_group = 0;
            }

            /*
            CAAD_data.apb_and_ab_door.apb_mode = 0;
            if (rdbSub000001_03.Checked)
            {
                CAAD_data.apb_and_ab_door.apb_mode = 1;
            }
            else if (rdbSub000001_04.Checked)
            {
                CAAD_data.apb_and_ab_door.apb_mode = 2;
            }
            */

            int[] apb_level_list = new int[intdoor_number];
            for (int i = 0; i < intdoor_number; i++)
            {
                apb_level_list[i] = 0;
            }
            SQL = String.Format("SELECT d.controller_door_index AS id,d_e.apb_level AS apb_level FROM door_extend AS d_e,door AS d,controller AS c WHERE (c.sn={0})AND(c.sn=d.controller_id)AND(d_e.door_id=d.id) ORDER BY d.controller_door_index;", controller_sn);//修改 『d.controller_door_index AS id』 2017/08/04
            MySqlDataReader Reader_Data2 = MySQL.GetDataReader(SQL);
            while (Reader_Data2.Read())
            {
                int id = 0, value = 0;
                id = Convert.ToInt32(Reader_Data2["id"].ToString());
                value = Convert.ToInt32(Reader_Data2["apb_level"].ToString());
                apb_level_list[(id - 1)] = value;//id-1 原因 門從1開始，但陣列從0開始
            }
            Reader_Data2.Close();
            for (int i = 0; i < intdoor_number; i++)
            {
                CAAD_data.apb_and_ab_door.apb_level_list.Add(apb_level_list[i]);
            }
            int[] apb_reset_timestamp_list = new int[8];
            for (int i = 0; i < apb_reset_timestamp_list.Length; i++)
            {
                apb_reset_timestamp_list[i] = 0;
            }
            int[] apb_reset_mode_list = new int[8];//為了支援時間旁邊的打勾元件 at 2017/08/18
            for (int i = 0; i < apb_reset_mode_list.Length; i++)
            {
                apb_reset_mode_list[i] = 0;
            }
            SQL = String.Format("SELECT d_e.apb_group_id AS apb_group_id,a_g_e.reset_time_1 AS r1,a_g_e.reset_time_2 AS r2,a_g_e.reset_time_3 AS r3,a_g_e.reset_time_4 AS r4,a_g_e.reset_time_5 AS r5,a_g_e.reset_time_6 AS r6,a_g_e.reset_time_7 AS r7,a_g_e.reset_time_8 AS r8 FROM door_extend AS d_e,door AS d,controller AS c,apb_group_extend AS a_g_e WHERE (a_g_e.apb_group_id=d_e.apb_group_id) AND (d_e.door_id=d.id) AND (d.controller_id=c.sn) AND c.sn={0} GROUP BY d_e.apb_group_id;", controller_sn);
            MySqlDataReader Reader_Data3 = MySQL.GetDataReader(SQL);
            while (Reader_Data3.Read())
            {
                int id;
                String StrBuf = "";
                DateTime r1, r2, r3, r4, r5, r6, r7, r8;
                id = Convert.ToInt32(Reader_Data3["apb_group_id"].ToString());
                StrBuf = Reader_Data3["r1"].ToString();//為了支援時間旁邊的打勾元件 at 2017/08/18
                r1 = Convert.ToDateTime(StrBuf.Substring(0,StrBuf.IndexOf("-")));
                apb_reset_mode_list[0] = ((StrBuf.IndexOf("-0") > 0) ? 0 : 1);

                StrBuf = Reader_Data3["r2"].ToString();//為了支援時間旁邊的打勾元件 at 2017/08/18
                r2 = Convert.ToDateTime(StrBuf.Substring(0,StrBuf.IndexOf("-")));
                apb_reset_mode_list[1] = ((StrBuf.IndexOf("-0") > 0) ? 0 : 1);

                StrBuf = Reader_Data3["r3"].ToString();//為了支援時間旁邊的打勾元件 at 2017/08/18
                r3 = Convert.ToDateTime(StrBuf.Substring(0,StrBuf.IndexOf("-")));
                apb_reset_mode_list[2] = ((StrBuf.IndexOf("-0") > 0) ? 0 : 1);

                StrBuf = Reader_Data3["r4"].ToString();//為了支援時間旁邊的打勾元件 at 2017/08/18
                r4 = Convert.ToDateTime(StrBuf.Substring(0,StrBuf.IndexOf("-")));
                apb_reset_mode_list[3] = ((StrBuf.IndexOf("-0") > 0) ? 0 : 1);

                StrBuf = Reader_Data3["r5"].ToString();//為了支援時間旁邊的打勾元件 at 2017/08/18
                r5 = Convert.ToDateTime(StrBuf.Substring(0,StrBuf.IndexOf("-")));
                apb_reset_mode_list[4] = ((StrBuf.IndexOf("-0") > 0) ? 0 : 1);

                StrBuf = Reader_Data3["r6"].ToString();//為了支援時間旁邊的打勾元件 at 2017/08/18
                r6 = Convert.ToDateTime(StrBuf.Substring(0,StrBuf.IndexOf("-")));
                apb_reset_mode_list[5] = ((StrBuf.IndexOf("-0") > 0) ? 0 : 1);

                StrBuf = Reader_Data3["r7"].ToString();//為了支援時間旁邊的打勾元件 at 2017/08/18
                r7 = Convert.ToDateTime(StrBuf.Substring(0,StrBuf.IndexOf("-")));
                apb_reset_mode_list[6] = ((StrBuf.IndexOf("-0") > 0) ? 0 : 1);

                StrBuf = Reader_Data3["r8"].ToString();//為了支援時間旁邊的打勾元件 at 2017/08/18
                r8 = Convert.ToDateTime(StrBuf.Substring(0,StrBuf.IndexOf("-")));
                apb_reset_mode_list[7] = ((StrBuf.IndexOf("-0") > 0) ? 0 : 1);

                apb_reset_timestamp_list[0] = (r1.Hour * 60 + r1.Minute) * 60;
                apb_reset_timestamp_list[1] = (r2.Hour * 60 + r2.Minute) * 60;
                apb_reset_timestamp_list[2] = (r3.Hour * 60 + r3.Minute) * 60;
                apb_reset_timestamp_list[3] = (r4.Hour * 60 + r4.Minute) * 60;
                apb_reset_timestamp_list[4] = (r5.Hour * 60 + r5.Minute) * 60;
                apb_reset_timestamp_list[5] = (r6.Hour * 60 + r6.Minute) * 60;
                apb_reset_timestamp_list[6] = (r7.Hour * 60 + r7.Minute) * 60;
                apb_reset_timestamp_list[7] = (r8.Hour * 60 + r8.Minute) * 60;
                break;
            }
            Reader_Data3.Close();
            for (int i = 0; i < apb_reset_timestamp_list.Length; i++)
            {
                CAAD_data.apb_and_ab_door.apb_reset_timestamp_list.Add(apb_reset_timestamp_list[i]);
                CAAD_data.apb_and_ab_door.apb_reset_mode_list.Add(apb_reset_mode_list[i]);//為了支援時間旁邊的打勾元件 at 2017/08/18
            }
            return CAAD_data;
        }

        private void butSub000301_24_Click(object sender, EventArgs e)//A.P.B參數直接用API套用
        {
            String SQL = "";
            int apb_group_id = -1;
            if (m_intDB2LeftSub000301_id < 0)//==-1
            {//add
                apb_group_id = AddAPB();
            }
            else
            {//save
                apb_group_id = m_intDB2LeftSub000301_id;
                SaveAPB();
            }

            SQL = String.Format("SELECT c_e.controller_sn AS sn,c_e.connetction_mode AS mode,c_e.connetction_address AS ip,c_e.port AS port FROM apb_door AS a_d,door AS d,controller_extend AS c_e WHERE d.controller_id=c_e.controller_sn AND a_d.door_id=d.id AND a_d.apb_group_id={0} GROUP BY c_e.controller_sn;", apb_group_id);//修正Getsy_dm_Controller_id 函數和相關呼叫點-SQL = String.Format("SELECT c_e.controller_sn AS sn,c_e.connetction_address AS ip,c_e.port AS port FROM apb_door AS a_d,door AS d,controller_extend AS c_e WHERE d.controller_id=c_e.controller_sn AND a_d.door_id=d.id AND a_d.apb_group_id={0} GROUP BY c_e.controller_sn;", apb_group_id);
            ArrayList AL_C_sn = new ArrayList();
            ArrayList AL_C_mode = new ArrayList();//修正Getsy_dm_Controller_id 函數和相關呼叫點-ArrayList AL_C_sn = new ArrayList();
            ArrayList AL_C_ip = new ArrayList();
            ArrayList AL_C_port = new ArrayList();
            MySqlDataReader ReaderData = MySQL.GetDataReader(SQL);
            while (ReaderData.Read())
            {
                AL_C_sn.Add(ReaderData["sn"].ToString());
                AL_C_mode.Add(ReaderData["mode"].ToString());//修正Getsy_dm_Controller_id 函數和相關呼叫點-AL_C_sn.Add(ReaderData["sn"].ToString());
                AL_C_ip.Add(ReaderData["ip"].ToString());
                AL_C_port.Add(ReaderData["port"].ToString());
            }
            ReaderData.Close();

            bool[] blnAns = new bool[AL_C_mode.Count];
            bool blnAns_All = true;
            int intsy_dm_Controller_id = -1;
            //---
            //SYDM和SYCG API呼叫並存實現
            //HW_Net_API.setAPIDomainURL(txtSys_01.Text, txtSys_07.Text);
            if (!m_changeSYCGMode)//SYDM
            {
                HW_Net_API.setAPIDomainURL(txtSys_01.Text, txtSys_07.Text);
            }
            else//SYCG
            {
                HW_Net_API.SYCG_setSYCGDomainURL();
            }
            //---SYDM和SYCG API呼叫並存實現

            //---
            //SYDM和SYCG API呼叫並存實現
            if (!m_changeSYCGMode)//SYDM
            {
                m_blnAPI = HW_Net_API.getController_Connection();
            }
            else//SYCG
            {
                m_blnAPI = HW_Net_API.SYCG_getSYDMList();
                if (m_blnAPI)
                {
                    HW_Net_API.m_Controller_Connection.controllers.Clear();
                    for (int l=0;l< m_Sydms.sydms.Count;l++)
                    {
                        HW_Net_API.SYCG_callSYDMCommand("SYDM_GET_CONTROLLER_CONNECTION", "", m_Sydms.sydms[l].identifier.ToString());
                    }
                }
            }
            //---SYDM和SYCG API呼叫並存實現	
            if (m_blnAPI)//if (HW_Net_API.getController_Connection())//實際聯結機器，太慢~if (HW_Net_API.getController())
            {
                for (int i = 0; i < AL_C_mode.Count; i++)
                {
                    intsy_dm_Controller_id = Getsy_dm_Controller_id(AL_C_mode[i].ToString(), AL_C_ip[i].ToString(), AL_C_port[i].ToString());
                    if (intsy_dm_Controller_id > 0)
                    {
                        CAAD_Controller CAAD_data = DB2CAAD_Controller(intsy_dm_Controller_id, AL_C_sn[i].ToString());
	                    //---
	                    //SYDM和SYCG API呼叫並存實現
	                    if(!m_changeSYCGMode)//SYDM
	                    {
		                    m_blnAPI = HW_Net_API.setController_Apb_Ab_Door(CAAD_data);
	                    }
	                    else//SYCG
	                    {
		                    //---
		                    //SYCG模式下-建立/暫存 當下要操作的SYDM ID
                            SQL = String.Format("SELECT c.sydm_id AS sydm_id FROM controller AS c WHERE (c.sn='{0}')", AL_C_sn[i].ToString());
		                    MySqlDataReader Readerd_SYDMid = MySQL.GetDataReader(SQL);
		                    while (Readerd_SYDMid.Read())
		                    {
			                    m_intSYDM_id = Convert.ToInt32(Readerd_SYDMid["sydm_id"].ToString());//SYCG模式下-建立/暫存 當下要操作的SYDM ID
			                    break;
		                    }
		                    Readerd_SYDMid.Close();
		                    //---SYCG模式下-建立/暫存 當下要操作的SYDM ID

		                    String StrCAAD_buf = parseJSON.composeJSON_Controller_Apb_Ab_Door(CAAD_data);
		                    m_blnAPI = HW_Net_API.SYCG_callSYDMCommand("SYDM_SET_CONTROLLER_APB_AB_DOOR", StrCAAD_buf, m_intSYDM_id.ToString());
	                    }
	                    //---SYDM和SYCG API呼叫並存實現
                        blnAns[i] = m_blnAPI;
                    }
                    else
                    {
                        blnAns[i] = false;
                    }
                }
                for (int i = 0; i < AL_C_sn.Count; i++)
                {
                    blnAns_All = blnAns_All && blnAns[i];
                }

                if (blnAns_All)
                {
                    MessageBox.Show(Language.m_StrbutSub000301_24Msg00, butSub000301_24.Text, MessageBoxButtons.OK, MessageBoxIcon.Information);
                }
                else
                {
                    String StrMsg = "";
                    for (int i = 0; i < AL_C_sn.Count; i++)
                    {
                        if(!blnAns[i])
                        {
                            StrMsg += AL_C_sn[i].ToString() + ",";
                        }
                    }
                    MessageBox.Show(Language.m_StrbutSub000301_24Msg01 + StrMsg + Language.m_StrbutSub000301_24Msg02, butSub000301_24.Text, MessageBoxButtons.OK, MessageBoxIcon.Error);
                }

            }
            else
            {
                MessageBox.Show(Language.m_StrbutSub000301_24Msg03, butSub000301_24.Text, MessageBoxButtons.OK, MessageBoxIcon.Error);
            }

            Leave_function();
        }

        public int m_intdgvSub000301_01_id = -1;
        private void dgvSub000301_01_SelectionChanged(object sender, EventArgs e)
        {
            try
            {
                int index = dgvSub000301_01.SelectedRows[0].Index;//取得被選取的第一列位置
                String Strid = dgvSub000301_01.Rows[index].Cells[1].Value.ToString();
                m_intdgvSub000301_01_id = Int32.Parse(Strid);
            }
            catch
            {
                m_intdgvSub000301_01_id = -1;
            }
        }

        private void butSub000301_21_Click(object sender, EventArgs e)//A.P.B批次執行
        {
            ArrayList ALSN = new ArrayList();
            ALSN.Clear();
            for (int i = 0; i < dgvSub000301_01.Rows.Count; i++)
            {
                String data = dgvSub000301_01.Rows[i].Cells[0].Value.ToString().ToLower();//抓取DataGridView欄位資料
                if (data == "true")
                {
                    ALSN.Add(dgvSub000301_01.Rows[i].Cells[1].Value.ToString());//抓 ID
                }
            }
            String SQL = "";
            switch (cmbSub000301_01.SelectedIndex)
            {
                case 0:
                    SQL = "";
                    for (int i = 0; i < ALSN.Count; i++)
                    {
                        SQL += "UPDATE apb_group SET status = 1,state=1 WHERE (id = " + ALSN[i].ToString() + ");";
                    }

                    break;
                case 1:
                    SQL = "";
                    for (int i = 0; i < ALSN.Count; i++)
                    {
                        SQL += "UPDATE apb_group SET status = 0,state=1 WHERE (id = " + ALSN[i].ToString() + ");";
                    }
                    break;
                case 2:
                    SQL = "";
                    for (int i = 0; i < ALSN.Count; i++)
                    {
                        SQL += String.Format("DELETE FROM apb_group WHERE id={0};DELETE FROM apb_group_extend WHERE apb_group_id={0};DELETE FROM apb_door WHERE apb_group_id={0};", ALSN[i].ToString());
                    }
                    break;
            }
            MySQL.InsertUpdateDelete(SQL);//新增資料程式

            initdgvSub000301_01();
        }

        private void rdbSub000301_01_CheckedChanged(object sender, EventArgs e)//A.P.B 類型選擇
        {
            //---
            //SYCG/SYDM模式下SYDM ID綁定程式
            if (m_changeSYCGMode)
            {
                if (m_ALSYDM_ID.Count > 0)
                {
                    m_StrAPBSydmid = (String)m_ALSYDM_ID[cmbSub000301_02.SelectedIndex];
                }
                else
                {
                    cmbSub000301_02.SelectedIndex = -1;
                    m_StrAPBSydmid = "0";
                }
            }
            else
            {
                cmbSub000301_02.SelectedIndex = -1;
                m_StrAPBSydmid = "0";
            }
            //---SYCG/SYDM模式下SYDM ID綁定程式

            int inttmp = -10;
            tvmSub000301_01.Nodes.Clear();
            tvmSub000301_02.Nodes.Clear();

            //--
            if (m_intDB2LeftSub000301_id > 0)//!=-1
            {
                inttmp = m_intDB2LeftSub000301_id;//
            }
            initvmSub000301_02(inttmp);
            //--

            //--
            if (rdbSub000301_01.Checked == true)
            {
                if (m_intDB2LeftSub000301_id < 0)//新增~APB門區模式 ==-1
                {
                    initvmSub000301_01(0, m_intDB2LeftSub000301_id);
                }
                else//編修~APB門區模式 
                {
                    initvmSub000301_01(2, m_intDB2LeftSub000301_id);
                }
            }
            else if (rdbSub000301_02.Checked == true)
            {

                if (m_intDB2LeftSub000301_id < 0)//新增~APB次數模式 ==-1
                {
                    initvmSub000301_01(1, m_intDB2LeftSub000301_id);
                }
                else//編修~APB次數模式
                {
                    initvmSub000301_01(3, m_intDB2LeftSub000301_id);
                }
            }
            //--

        }

        Tree_Node m_Tree_Node_select = null;
        private void tvmSub000301_02_AfterSelect(object sender, TreeViewEventArgs e)//選擇要調整 APB Level 的門區事件
        {
            for (int i = 0; i < tvmSub000301_02.Nodes.Count; i++)//清除上次 txtSub000301_05_Button_Click 的狀態 at 2017/08/03
            {
                ((Tree_Node)tvmSub000301_02.Nodes[i]).BackColor = Color.White;
                ((Tree_Node)tvmSub000301_02.Nodes[i]).ForeColor = Color.Black;
            }
            String SQL;
            m_Tree_Node_select = null;
            m_Tree_Node_select = ((Tree_Node)tvmSub000301_02.SelectedNode);
            int id = m_Tree_Node_select.m_id;
            SQL = String.Format("SELECT apb_level FROM door_extend WHERE door_id={0};", id);
            MySqlDataReader ReaderData = MySQL.GetDataReader(SQL);
            while (ReaderData.Read())
            {
                txtSub000301_05.Value = Convert.ToInt32(ReaderData["apb_level"].ToString());
                break;
            }
            ReaderData.Close();
            //MessageBox.Show(id + "");
        }

        private void txtSub000301_05_Button_Click(object sender, EventArgs e)//調整 APB Level 門區事件
        {
            if (m_Tree_Node_select != null)
            {
                String SQL = "";
                int id = m_Tree_Node_select.m_id;
                m_Tree_Node_select.BackColor = SystemColors.Highlight;//設定是選擇的UI狀態
                m_Tree_Node_select.ForeColor = SystemColors.HighlightText;//設定是選擇的UI狀態
                SQL = String.Format("UPDATE door_extend SET apb_level={0},state=1 WHERE door_id={1};", txtSub000301_05.Value, id);
                MySQL.InsertUpdateDelete(SQL);//更新資料程式
                //MessageBox.Show(id + "," + txtSub000301_05.Value);
            }
        }

        //Sub000301_end

        //Sys_start
        private void txtSys_04_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (m_blnLoad)
            {
                m_Settings.m_StrLanguage = "" + txtSys_04.SelectedIndex;
                //Language.initVar();
                Language.ReadLangSet(txtSys_04.SelectedIndex);
                Language.ALStr2Var();

                //---
                //將設定檔變成語系檔內容
                HW_Net_API.getHWInfo();//讀取機型到記憶體
                HW_Net_API.getCardType();//讀取卡片類型
                HW_Net_API.getRecordStatus();//撰寫把record_status.csv匯入資料庫中
                HW_Net_API.getfingerprint_type();//匯入fingerprint_type
                //---將設定檔變成語系檔內容

                initTabPage();
                initWelcomeUI();
                initMain00UI();
                #if (!Delta_Tool)//修正隱藏UI功能把SYDM按鈕在切換至台達板時也要隱藏
	                JLMB_Main0004.Visible = true;
                #else
                    JLMB_Main0004.Visible = false;
                #endif
                initMain01UI();
                initMain02UI();
                initMain03UI();
                initMain04UI();
                initSub0000UI();
                initSub000001UI(false);//修正所以語系切換，都不能執行DB相關，所以DB相關都要可以獨立執行
                initSub0001UI(false);//修正所以語系切換，都不能執行DB相關，所以DB相關都要可以獨立執行
                initSub000100UI();
                initSub000101UI();
                initSub0002UI(false);//修正所以語系切換，都不能執行DB相關，所以DB相關都要可以獨立執行
                initSub000200UI(false);//修正所以語系切換，都不能執行DB相關，所以DB相關都要可以獨立執行
                initSub0003UI(false);//修正所以語系切換，都不能執行DB相關，所以DB相關都要可以獨立執行
                initSub000301UI(false);//修正所以語系切換，都不能執行DB相關，所以DB相關都要可以獨立執行
                //---
                //開發SYDM UI-系統載入時『列表m_tabSub0004』和『編輯m_tabSub000400』元件基本初始化
                initSub0004UI(false);//修正所以語系切換，都不能執行DB相關，所以DB相關都要可以獨立執行
                initSub000400UI();
                //---
                //---
                //開發報表 UI-系統預設『列表m_tabSub0300』元件基本初始化
                initSub0300UI(false);//修正所以語系切換，都不能執行DB相關，所以DB相關都要可以獨立執行
                //---
                initSub0400UI(false);//抓取指紋UI初始化 //修正所以語系切換，都不能執行DB相關，所以DB相關都要可以獨立執行

                //--
                //add 2017/10/24
                if(!m_changeToolMode)
                {
                    initSub0100UI();
                    initSub0101UI();
                    initSub0102UI();
                    initSub0103UI();
                    initSub0104UI(false);//修正所以語系切換，都不能執行DB相關，所以DB相關都要可以獨立執行
                    initSub0200UI(false);//修正所以語系切換，都不能執行DB相關，所以DB相關都要可以獨立執行
                    initSub010000UI();
                    initSub010100UI(false);//修正所以語系切換，都不能執行DB相關，所以DB相關都要可以獨立執行
                    initSub010200UI();
                    initSub010400UI(false);//修正所以語系切換，都不能執行DB相關，所以DB相關都要可以獨立執行
                    initSub020000UI(false);//修正所以語系切換，都不能執行DB相關，所以DB相關都要可以獨立執行
                    initSub0201UI();
                    initSub0202UI();
                    initSub0203UI(false);//修正所以語系切換，都不能執行DB相關，所以DB相關都要可以獨立執行
                    initSub020300UI();
                }
                //--
                initSysUI();
                PromptTextBox_initTipText();
                m_OutlookBar1.Initialize();//menu_step03
                CreateMenu();
                m_OutlookBar1.SelectedBand = m_OutlookBar1.intLastPage;//2017/10/31 //2017/01/08 預設開啟的功能選單
                m_tabMain.SelectedTab = m_tabSys;//2017/02/24 設定起始頁

                //---
                //非中文語系要綁定字型
                /*
                if (txtSys_04.SelectedIndex >= 2)
                {

                    List<string> fonts = new List<string>();
                    foreach (FontFamily font in System.Drawing.FontFamily.Families)
                    {
                        fonts.Add(font.Name);
                    }
                    for (int i = 0; i < fonts.Count; i++)
                    {
                        if ((fonts[i] == "Arial Narrow")) //2017/03/01 || (fonts[i] == nowFont.Name))
                        {
                            m_intLastLangIndex = txtSys_05.SelectedIndex;
                            txtSys_05.SelectedIndex = i;
                            break;
                        }
                    }
                }
                else
                {
                    txtSys_05.SelectedIndex = m_intLastLangIndex;
                }
                */ 
                //---非中文語系要綁定字型
                
                //---
                //按照『V8 功能選單』一個一個改 ~ 把系統頁面的字型設定成按照語系自動調整
                String StrFontName = "";
                switch (txtSys_04.SelectedIndex)
                {
                    case 0://繁體
                        StrFontName = "微軟正黑體";
                        break;
                    case 1://簡體
                        StrFontName = "微软雅黑";
                        break;
                    case 2://英文
                        StrFontName = "Arial";
                        break;
                    case 3://其他
                        StrFontName = "Arial";
                        break;
                }
                List<string> fonts = new List<string>();
                foreach (FontFamily font in System.Drawing.FontFamily.Families)
                {
                    fonts.Add(font.Name);
                }
                for (int i = 0; i < fonts.Count; i++)
                {
                    if ((fonts[i] == StrFontName)) //2017/03/01 || (fonts[i] == nowFont.Name))
                    {
                        m_intLastLangIndex = txtSys_05.SelectedIndex;
                        txtSys_05.SelectedIndex = i;
                        break;
                    }
                }
                txtSys_06.SelectedIndex = 0;
                //---按照『V8 功能選單』一個一個改 ~ 把系統頁面的字型設定成按照語系自動調整
            }
        }

        private void butSys_04_Click(object sender, EventArgs e)//DB匯入
        {
            //---
            //Remote/Local DB 資料交換變成有等待動畫-DB匯入
            //m_ExMySQL.DownloadDB_user();
            //m_ExMySQL.DownloadDB_other();
            Animation.createThreadAnimation(butSys_04.Text, Animation.Thread_DownloadDB);
            //---Remote/Local DB 資料交換變成有等待動畫-DB匯入

            MessageBox.Show(Language.m_StrSysMsg01, butSys_04.Text, MessageBoxButtons.OK, MessageBoxIcon.Information);
        }

        private void butSys_05_Click(object sender, EventArgs e)//DB匯出
        {
            //---
            //Remote/Local DB 資料交換變成有等待動畫-DB匯出
            //m_ExMySQL.UploadDB_user();
            //m_ExMySQL.UploadDB_other();
            Animation.createThreadAnimation(butSys_05.Text, Animation.Thread_UploadDB);
            //---Remote/Local DB 資料交換變成有等待動畫-DB匯出
            MessageBox.Show(Language.m_StrSysMsg02, butSys_05.Text, MessageBoxButtons.OK, MessageBoxIcon.Information);
        }

        private void txtSys_05_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (m_blnLoad)
            {
                m_Settings.m_StrFontName = txtSys_05.Text;
                m_Settings.m_StrFontSize = txtSys_06.Text;
                this.Font = new Font(m_Settings.m_StrFontName, Int32.Parse(m_Settings.m_StrFontSize));
            }
        }

        private void butSys_02_Click(object sender, EventArgs e)
        {
            //--
            //修正儲存系統設定檔按鈕功能
            m_Settings.m_StrIP = txtSys_01.Text + ":" + txtSys_07.Text;//此欄位本來只有IP，2017/08/01 才知道要可以設定PORT，因此做了修改
            m_Settings.m_StrUser = txtSys_02.Text;
            m_Settings.m_StrPassword = txtSys_03.Text;
            m_Settings.m_StrFontName = txtSys_05.Text;
            m_Settings.m_StrFontSize = txtSys_06.Text;
            //--
            m_Settings.saveSettingXML();
        }

        private void ckbSys_01_CheckedChanged(object sender, EventArgs e)
        {
            m_Settings.m_StrAutoDisplay = ckbSys_01.Checked.ToString();
        }

        public bool testSYDMConnect()//為了實作出 TOOL和Workstation 連線測試差異，該函數是TOOL at 2017/08/23
        {
            //---
            //SYDM和SYCG API呼叫並存實現
            //HW_Net_API.setAPIDomainURL(txtSys_01.Text, txtSys_07.Text);
            if (!m_changeSYCGMode)//SYDM
            {
                HW_Net_API.setAPIDomainURL(txtSys_01.Text, txtSys_07.Text);
            }
            else//SYCG
            {
                HW_Net_API.SYCG_setSYCGDomainURL();
            }
            //---SYDM和SYCG API呼叫並存實現

            //---
            //SYDM和SYCG API呼叫並存實現
            if (!m_changeSYCGMode)//SYDM
            {
                m_blnAPI = HW_Net_API.getController_Connection();

            }
            else//SYCG
            {
                m_blnAPI = HW_Net_API.SYCG_getSYDMList();
                if (m_blnAPI)
                {
                    HW_Net_API.m_Controller_Connection.controllers.Clear();
                    for (int l = 0; l < m_Sydms.sydms.Count; l++)
                    {
                        HW_Net_API.SYCG_callSYDMCommand("SYDM_GET_CONTROLLER_CONNECTION", "", m_Sydms.sydms[l].identifier.ToString());
                    }
                }
            }
            //---SYDM和SYCG API呼叫並存實現	
            if(m_blnAPI)//if (HW_Net_API.getController_Connection())
            {
                m_Settings.m_StrIP = txtSys_01.Text + ":" + txtSys_07.Text;//此欄位本來只有IP，2017/08/01 才知道要可以設定PORT，因此做了修改
                m_Settings.m_StrUser = txtSys_02.Text;
                m_Settings.m_StrPassword = txtSys_03.Text;

                return true;//MessageBox.Show(Language.m_StrConnectMsg01, Language.m_StrConnectMsg00, MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
            else
            {
                return false;//MessageBox.Show(Language.m_StrConnectMsg02, Language.m_StrConnectMsg00, MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        //---
        //新增SYCG模式切換模式
        public bool testSYCGConnect()
        {
            bool blnAns = true;

            blnAns = m_ExMySQL.CheckMySQL(txtSys_01.Text, txtSys_07.Text, txtSys_02.Text, txtSys_03.Text);//增加外部SERVER測試函數
            //---
            //抓下SERVER上MySQL的資料
            if (blnAns==true)
            {
                //--
                //修正儲存系統設定檔按鈕功能
                m_Settings.m_StrIP = txtSys_01.Text + ":" + txtSys_07.Text;//此欄位本來只有IP，2017/08/01 才知道要可以設定PORT，因此做了修改
                m_Settings.m_StrUser = txtSys_02.Text;
                m_Settings.m_StrPassword = txtSys_03.Text;
                m_Settings.m_StrFontName = txtSys_05.Text;
                m_Settings.m_StrFontSize = txtSys_06.Text;
                //--
                m_Settings.saveSettingXML();

                blnAns = m_ExMySQL.DownloadDBTable("config");
            }
            //---
            return blnAns;
        }
        //---

        private void butSys_01_Click(object sender, EventArgs e)//WEB連線測試
        {
            /*
            //ex $url = 'http://127.0.0.1:81/v8/v8_log/login_submit';
            //WEB API 測試程式碼
            m_CS_PHP.m_StrDomain = "http://" + txtSys_01 + ":81/";//m_CS_PHP.m_StrDomain = "http://" + txtSys_01+"/";
            String Data=m_CS_PHP.loginPHP("v8/v8_log/login_submit", txtSys_02.Text);
            String StrVar="";
            if (Data.Contains("\"result\":true"))
            {
                m_Settings.m_StrIP = txtSys_01.Text;
                m_Settings.m_StrUser = txtSys_02.Text;
                m_Settings.m_StrPassword = txtSys_03.Text;

                MessageBox.Show(Language.m_StrConnectMsg01, Language.m_StrConnectMsg00, MessageBoxButtons.OK, MessageBoxIcon.Information);

                StrVar = "{\"data\":\"" + Remote_DB.Cleancontroller() + "\"}";//清空資料表
                Data = m_CS_PHP.runPHP("v8/exe/delete", StrVar);

                StrVar = "{\"data\":\"" + Remote_DB.Getcontroller() + "\"}";//查詢是否全部清空
                Data = m_CS_PHP.runPHP("v8/exe/select", StrVar);

                if (Remote_DB.Updatecontroller())
                {
                    StrVar = "{\"data\":\"" + Remote_DB.m_StrRemoteSQL + "\"}"; //新增資料
                    Data = m_CS_PHP.runPHP("v8/exe/insert", StrVar);

                    StrVar = "{\"data\":\"" + Remote_DB.Getcontroller() + "\"}";//查詢新增結果
                    Data = m_CS_PHP.runPHP("v8/exe/select", StrVar);
                    Data = Data.Replace("null", "\"\"");
                    Remote_controller _controller = JsonConvert.DeserializeObject<Remote_controller>(Data);

                }
            }
            */
            //String Strip=HW_Net_API.long2ip(3456215232,true);
            //Int32 Int32_ip = HW_Net_API.ip2long(Strip, true);

            /*
            HW_Net_API.setAPIDomainURL(txtSys_01.Text, txtSys_07.Text);
            if (HW_Net_API.getController_Connection())
            {
                m_Settings.m_StrIP = txtSys_01.Text + ":" + txtSys_07.Text;//此欄位本來只有IP，2017/08/01 才知道要可以設定PORT，因此做了修改
                m_Settings.m_StrUser = txtSys_02.Text;
                m_Settings.m_StrPassword = txtSys_03.Text;

                MessageBox.Show(Language.m_StrConnectMsg01, Language.m_StrConnectMsg00, MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
            else
            {
                MessageBox.Show(Language.m_StrConnectMsg02, Language.m_StrConnectMsg00, MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            */
            if (m_changeSYCGMode)//新增SYCG模式切換模式
            {
                Animation.createThreadAnimation(butSys_01.Text, Animation.Thread_testSYCGConnect);//系統頁面連線測試變成有等待動畫-SYCG等待動畫
                if (Animation.m_blnAns)//系統頁面連線測試變成有等待動畫-SYCG等待動畫if (testSYCGConnect())//會取得SYCG的URL
                {
                    butSys_04.Enabled = true;
                    butSys_05.Enabled = true;
                    txtSys_08.SelectedIndex = 0;//SYCG測試連線時要按照測試結果修改下方下拉式選單狀態
                    MessageBox.Show(Language.m_StrConnectMsg01, Language.m_StrConnectMsg00, MessageBoxButtons.OK, MessageBoxIcon.Information);
                }
                else
                {
                    butSys_04.Enabled = false;
                    butSys_05.Enabled = false;
                    txtSys_08.SelectedIndex = 1;//SYCG測試連線時要按照測試結果修改下方方下拉式選單狀態
                    MessageBox.Show(Language.m_StrConnectMsg02, Language.m_StrConnectMsg00, MessageBoxButtons.OK, MessageBoxIcon.Error);
                }

            }
            else//SYDM
            {
                if (m_changeToolMode)
                {//Tool
                    txtSys_08.SelectedIndex = 0;
                    Animation.createThreadAnimation(butSys_01.Text, Animation.Thread_testSYDMConnect);//系統頁面連線測試變成有等待動畫-SYDM等待動畫
                    if (Animation.m_blnAns)//系統頁面連線測試變成有等待動畫-SYDM等待動畫 if (testSYDMConnect())
                    {
                        MessageBox.Show(Language.m_StrConnectMsg01, Language.m_StrConnectMsg00, MessageBoxButtons.OK, MessageBoxIcon.Information);
                    }
                    else
                    {
                        MessageBox.Show(Language.m_StrConnectMsg02, Language.m_StrConnectMsg00, MessageBoxButtons.OK, MessageBoxIcon.Error);
                    }
                }
                else
                {//Workstation
                    Animation.createThreadAnimation(butSys_01.Text, Animation.Thread_testSYDMConnect);//系統頁面連線測試變成有等待動畫-SYDM等待動畫
                    if (Animation.m_blnAns)//系統頁面連線測試變成有等待動畫-SYDM等待動畫 if (testSYDMConnect())
                    {
                        txtSys_08.SelectedIndex = 0;
                        MessageBox.Show(Language.m_StrConnectMsg01, Language.m_StrConnectMsg00, MessageBoxButtons.OK, MessageBoxIcon.Information);
                    }
                    else
                    {
                        txtSys_08.SelectedIndex = 1;
                        MessageBox.Show(Language.m_StrConnectMsg02, Language.m_StrConnectMsg00, MessageBoxButtons.OK, MessageBoxIcon.Error);
                    }
                    //MessageBox.Show(Language.m_StrConnectMsg02, Language.m_StrConnectMsg00, MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
            }

            //---
            //連線按鈕需要能有顏色識別上次連線狀態
            if (Animation.m_blnAns)
            {
                butSys_01.BackColor = Color.GreenYellow;
            }
            else
            {
                butSys_01.BackColor = Color.Red;
            }
            //---連線按鈕需要能有顏色識別上次連線狀態

            labSys_11.Text = txtSys_08.Text;//連線狀態元件換成純文字顯示
        }

        private void txtSys_07_KeyPress(object sender, KeyPressEventArgs e)//系統設定頁面 PORT的防呆~只能輸入數字
        {
            if (e.KeyChar == 8)//刪除鍵要直接允許
            {
                e.Handled = false;
            }
            else
            {
                if (e.KeyChar >= '0' && e.KeyChar <= '9')//限制0~9
                {
                    e.Handled = false;
                }
                else
                {
                    e.Handled = true;
                }
            }
        }

        private void txtSys_07_KeyUp(object sender, KeyEventArgs e)//系統設定頁面 PORT的防呆~只能輸入1~65535
        {
            int temp = 0;
            try
            {
                temp = Convert.ToInt32(txtSys_07.Text);
            }
            catch
            {
                temp = 24408;
            }
            if (!(temp >= 1 && temp <= 65535))
            {
                temp = 24408;
            }
            txtSys_07.Text = "" + temp;
        }

        //Sys_end

        //Sub0100_start
        public int m_intuser_id = -1;//紀錄user_id
        public int m_intdep_id = -2;
        private void dgvSub0100_01_DoubleClick(object sender, EventArgs e)//at 2017/09/15
        {
            butSub0100_01.PerformClick();
        }
        private void butSub0100_01_Click(object sender, EventArgs e)//修改人員
        {
            FileLib.DeleteFile("temp.png");//徹底刪除人員車輛照片暫存檔-2018/04/02防呆用

            //if(m_intuser_id <0)
            //{
                try
                {
                    String SQL;
                    int index = dgvSub0100_01.SelectedRows[0].Index;//取得被選取的第一列位置
                    String Struser_id = dgvSub0100_01.Rows[index].Cells[1].Value.ToString();
                    m_intuser_id = Int32.Parse(Struser_id);
                    SQL = String.Format("SELECT id FROM department WHERE name='{0}';", dgvSub0100_01.Rows[index].Cells[5].Value.ToString());
                    MySqlDataReader DataReader = MySQL.GetDataReader(SQL);
                    while (DataReader.Read())
                    {
                        m_intdep_id = Convert.ToInt32(DataReader["id"].ToString());
                        break;
                    }
                    DataReader.Close();
                }
                catch
                {
                }
            //}
            if (m_intuser_id > 0)
            {
                FileLib.DeleteFile("temp.png");
                String SQL = "";
                m_TPOld = m_tabMain.SelectedTab;//--2017/02/22 製作返回按鈕功能
                TabPage_Push();//m_StackTPOld.Push(m_TPOld);//--2017/02/22 製作返回按鈕功能
                String StrImageData = "";

                initSub010000UI();
                get_show_UserCards(m_intuser_id);//取得人員的卡片列表

                SQL = String.Format("SELECT * FROM user WHERE id={0};", m_intuser_id);
                MySqlDataReader DataReader = MySQL.GetDataReader(SQL);
                while (DataReader.Read())
                {
                    //---
                    //人機介面要能設定是否能登錄 ~ DB關聯(載入時)
                    if (DataReader["status"].ToString() == "1")
                    {
                        ckbSub010000_01.Checked = true;
                    }
                    else
                    {
                        ckbSub010000_01.Checked = false;
                    }
                    //---人機介面要能設定是否能登錄 ~ DB關聯(載入時)

                    txtSub010000_01.Text = DataReader["name"].ToString();
                    
                    imgSub010000_01.Image = null;

                    txtSub010000_02.Text = DataReader["alias_name"].ToString();

                    if(DataReader["gender"].ToString().Length>0)//try
                    {
                        if (Convert.ToInt32(DataReader["gender"].ToString()) > 0)
                        {
                            rdbSub010000_01.Checked = true;
                        }
                        else
                        {
                            rdbSub010000_02.Checked = true;
                        }
                    }
                    else//catch
                    {
                        rdbSub010000_01.Checked = true;
                    }
                    
                    String StrBuf = ""+m_intdep_id;
                    for (int i = 0; i < m_ALDepartment_ID.Count; i++)
                    {
                        if (StrBuf == m_ALDepartment_ID[i].ToString())
                        {
                            cmbSub010000_02.SelectedIndex = i;
                            break;
                        }
                    }

                    txtSub010000_04.Text = DataReader["attribute"].ToString();

                    try
                    {
                        txtSub010000_05.Value = Convert.ToDateTime(DataReader["birthday"].ToString());
                    }
                    catch
                    {
                        txtSub010000_05.Value = DateTime.Now;//User資料匯入時會沒有填生日的狀況BUG修正
                    }

                    txtSub010000_06.Text = DataReader["emp_no"].ToString();

                    txtSub010000_07.Text = DataReader["security_id"].ToString();

                    txtSub010000_08.Text = DataReader["passport_id"].ToString();

                    txtSub010000_09.Text = DataReader["office_tel"].ToString();

                    txtSub010000_10.Text = DataReader["home_tel"].ToString();

                    txtSub010000_11.Text = DataReader["cell_phone"].ToString();

                    txtSub010000_12.Text = DataReader["emergency_contactor"].ToString();

                    txtSub010000_13.Text = DataReader["email"].ToString();

                    txtSub010000_14.Text = DataReader["emergency_tel"].ToString();

                    txtSub010000_15.Text = DataReader["family_address"].ToString();

                    txtSub010000_16.Text = DataReader["contact_address"].ToString();

                    txtSub010000_17.Text = DataReader["note"].ToString();

                    StrImageData = DataReader["pic"].ToString();
                    if (StrImageData.Length > 0)
                    {
                        String StrDestFilePath = FileLib.path + "\\temp.png";

                        byte[] data = Convert.FromBase64String(StrImageData);
                        FileLib.CreateFile(StrDestFilePath, data);

                        //--
                        //c# 圖片檔讀取：非鎖定檔方法~http://fecbob.pixnet.net/blog/post/38125005
                        FileStream fs = File.OpenRead(StrDestFilePath); //OpenRead
                        int filelength = 0;
                        filelength = (int)fs.Length; //獲得檔長度
                        Byte[] image = new Byte[filelength]; //建立一個位元組陣列
                        fs.Read(image, 0, filelength); //按位元組流讀取
                        System.Drawing.Image result = System.Drawing.Image.FromStream(fs);
                        fs.Close();
                        //--

                        imgSub010000_01.Image = result;//Image.FromFile(StrDestFilePath);
                    }

                    break;
                }
                DataReader.Close();

                txtSub010000_06.Enabled = false;//新增人員時可修改USER 工號但在編輯時禁止修改
                m_tabSub010000.Parent = m_tabMain;
                m_tabMain.SelectedTab = m_tabSub010000;

                //--
                //add at 2017/10/11
                m_Sub010000ALInit.Clear();
                m_Sub010000ALRight.Clear();//add at 2017/10/18
                m_Sub010000ALInit.Add(txtSub010000_01.Text);
                m_Sub010000ALInit.Add(txtSub010000_02.Text);
                m_Sub010000ALInit.Add(rdbSub010000_01.Checked.ToString());
                m_Sub010000ALInit.Add(rdbSub010000_02.Checked.ToString());
                m_Sub010000ALInit.Add(cmbSub010000_02.SelectedIndex + "");
                m_Sub010000ALInit.Add(txtSub010000_04.Text);
                m_Sub010000ALInit.Add(txtSub010000_05.Value.ToString("yyyy-MM-dd HH:mm"));
                m_Sub010000ALInit.Add(txtSub010000_06.Text);
                m_Sub010000ALInit.Add(txtSub010000_07.Text);
                m_Sub010000ALInit.Add(txtSub010000_08.Text);
                m_Sub010000ALInit.Add(txtSub010000_09.Text);
                m_Sub010000ALInit.Add(txtSub010000_10.Text);
                m_Sub010000ALInit.Add(txtSub010000_11.Text);
                m_Sub010000ALInit.Add(txtSub010000_12.Text);
                m_Sub010000ALInit.Add(txtSub010000_13.Text);
                m_Sub010000ALInit.Add(txtSub010000_14.Text);
                m_Sub010000ALInit.Add(txtSub010000_15.Text);
                m_Sub010000ALInit.Add(txtSub010000_16.Text);
                m_Sub010000ALInit.Add(txtSub010000_17.Text);
                m_Sub010000ALInit.Add(ckbSub010000_01.Checked.ToString());//人機介面要能設定是否能登錄 ~ UI變化紀錄

                if (StrImageData.Length > 0)
                {
                    m_Sub010000ALInit.Add(StrImageData);
                }

                for (int i = 0; i < dgvSub010000_01.Rows.Count; i++)
                {
                    m_Sub010000ALInit.Add(dgvSub010000_01.Rows[i].Cells[1].Value.ToString());
                    m_Sub010000ALRight.Add(dgvSub010000_01.Rows[i].Cells[1].Value.ToString());//add at 2017/10/18
                }
                //--
            }
        }

        private void butSub0100_02_Click(object sender, EventArgs e)//新增人員
        {
            FileLib.DeleteFile("temp.png");//徹底刪除人員車輛照片暫存檔-2018/04/02防呆用

            m_intuser_id = -10;
            m_TPOld = m_tabMain.SelectedTab;//--2017/02/22 製作返回按鈕功能
            TabPage_Push();//m_StackTPOld.Push(m_TPOld);//--2017/02/22 製作返回按鈕功能

            initSub010000UI();
            m_tabSub010000.Parent = m_tabMain;
            m_tabMain.SelectedTab = m_tabSub010000;

            //--
            //add at 2017/10/11
            m_Sub010000ALInit.Clear();
            m_Sub010000ALInit.Add(txtSub010000_01.Text);
            m_Sub010000ALInit.Add(txtSub010000_02.Text);
            m_Sub010000ALInit.Add(rdbSub010000_01.Checked.ToString());
            m_Sub010000ALInit.Add(rdbSub010000_02.Checked.ToString());
            m_Sub010000ALInit.Add(cmbSub010000_02.SelectedIndex + "");
            m_Sub010000ALInit.Add(txtSub010000_04.Text);
            m_Sub010000ALInit.Add(txtSub010000_05.Value.ToString("yyyy-MM-dd HH:mm"));
            m_Sub010000ALInit.Add(txtSub010000_06.Text);
            m_Sub010000ALInit.Add(txtSub010000_07.Text);
            m_Sub010000ALInit.Add(txtSub010000_08.Text);
            m_Sub010000ALInit.Add(txtSub010000_09.Text);
            m_Sub010000ALInit.Add(txtSub010000_10.Text);
            m_Sub010000ALInit.Add(txtSub010000_11.Text);
            m_Sub010000ALInit.Add(txtSub010000_12.Text);
            m_Sub010000ALInit.Add(txtSub010000_13.Text);
            m_Sub010000ALInit.Add(txtSub010000_14.Text);
            m_Sub010000ALInit.Add(txtSub010000_15.Text);
            m_Sub010000ALInit.Add(txtSub010000_16.Text);
            m_Sub010000ALInit.Add(txtSub010000_17.Text);
            m_Sub010000ALInit.Add(ckbSub010000_01.Checked.ToString());//人機介面要能設定是否能登錄 ~ UI變化紀錄
            //--
        }

        //---
        //人員匯入改成執行緒模式
        public String m_StrImportUserCSVPath = "";
        public void ImportUserCSV()
        {
            // 建立檔案串流（@ 可取消跳脫字元 escape sequence）
            StreamReader sr = new StreamReader(m_StrImportUserCSVPath);
            String SQL = "";
            int intindex = 0;
            String StrTitle = "";
            while (!sr.EndOfStream)// 每次讀取一行，直到檔尾
            {
                String line = sr.ReadLine();// 讀取文字到 line 變數

                //--
                //人員資料表全部(id和state除外)匯入
                String Data = "";
                if (intindex == 0)
                {
                    StrTitle = line;
                    //---
                    //人員匯入/匯出CSV功能將[username,password,auth_group_id,del]欄位剔除
                    StrTitle += ",username,password";
                    //---人員匯入/匯出CSV功能將[username,password,auth_group_id,del]欄位剔除
                }
                else
                {
                    if ((StrTitle.Length > 0) && (line.Length > 0))
                    {
                        //--
                        //車/人匯入時要有預設部門
                        string[] strs = line.Split(',');
                        String Strname, Stremp_no;
                        Strname = strs[0];//CSV順序
                        Stremp_no = strs[5];//人員匯入/匯出CSV功能將[username,password,auth_group_id,del]欄位剔除- strs[9];//CSV順序//工號
                        //--

                        Data = "'" + line + "'";
                        Data = Data.Replace(",", "','");

                        //---
                        //人員匯入/匯出CSV功能將[username,password,auth_group_id,del]欄位剔除
                        String Strusername, Strpassword;
                        Strusername = Stremp_no;//工號
                        Strpassword = Web_encrypt.MD5_BASE64forPHP(Stremp_no);
                        Data += String.Format(",'{0}','{1}'", Strusername, Strpassword);
                        //---人員匯入/匯出CSV功能將[username,password,auth_group_id,del]欄位剔除

                        /*車/人匯入時要有預設部門
                        SQL += String.Format("INSERT INTO user ({0}) VALUES ({1});", StrTitle, Data);
                        */

                        //--
                        //車/人匯入時要有預設部門
                        m_intuser_id = -10;
                        //---

                        //---
                        //人員重複匯入防呆機制
                        //SQL = String.Format("INSERT INTO user ({0}) VALUES ({1});", StrTitle, Data);
                        //bool blnAns = MySQL.InsertUpdateDelete(SQL);//新增資料程式
                        bool blnAns = false;
                        SQL = String.Format("SELECT id FROM user WHERE emp_no='{1}';", Strname, Stremp_no);
                        MySqlDataReader Readercheck = MySQL.GetDataReader(SQL);//判斷資料是否已存在
                        if (!Readercheck.HasRows)
                        {
                            Readercheck.Close();
                            SQL = String.Format("INSERT INTO user ({0}) VALUES ({1});", StrTitle, Data);
                            blnAns = MySQL.InsertUpdateDelete(SQL);//新增資料程式
                        }
                        else
                        {
                            Readercheck.Close();
                            blnAns = false;
                        }
                        //---人員重複匯入防呆機制

                        if (blnAns == true)
                        {
                            SQL = String.Format("SELECT id FROM user WHERE name='{0}' AND emp_no='{1}';", Strname, Stremp_no);
                            MySqlDataReader DataReader = MySQL.GetDataReader(SQL);//新增資料
                            while (DataReader.Read())
                            {
                                m_intuser_id = Convert.ToInt32(DataReader["id"].ToString());
                            }
                            DataReader.Close();
                            if (m_intuser_id > 0)
                            {
                                SQL = String.Format("INSERT INTO department_detail (dep_id,state,user_id) VALUES ({0},{1},{2});", -1, 1, m_intuser_id);
                                MySQL.InsertUpdateDelete(SQL);//新增資料
                            }
                        }
                        //--
                    }
                }
                //--

                intindex++;

            }
            sr.Close();// 關閉串流
        }
        private void butSub0100_03_Click(object sender, EventArgs e)//人員匯入[csv]
        {
	        OpenFileDialog openFileDialog1 = new OpenFileDialog();
	        openFileDialog1.Filter = "CSV File|*.csv";
	        openFileDialog1.Title = "Open an CSV";
	        openFileDialog1.RestoreDirectory = true;
            if (openFileDialog1.ShowDialog() == DialogResult.OK)
            {
                m_StrImportUserCSVPath = openFileDialog1.FileName.ToString();
                Animation.createThreadAnimation(butSub0100_03.Text, Animation.Thread_ImportUserCSV);
                get_show_Users();//取得人員列表
            }
            
        }
        private void butSub0100_03_Click_XX(object sender, EventArgs e)//人員匯入[csv]-寫在按鈕內
        {
            String StrPath;
            //String Stremp_no, Strsecurity_id, Strname,Strdep_id, Strattribute, Strbirthday;
            String SQL = "";
            OpenFileDialog openFileDialog1 = new OpenFileDialog();
            openFileDialog1.Filter = "CSV File|*.csv";
            openFileDialog1.Title = "Open an CSV";
            openFileDialog1.RestoreDirectory = true;
            int intindex = 0;
            if (openFileDialog1.ShowDialog() == DialogResult.OK)
            {
                StrPath = openFileDialog1.FileName.ToString();
                // 建立檔案串流（@ 可取消跳脫字元 escape sequence）
                StreamReader sr = new StreamReader(StrPath);
                String StrTitle = "";
                while (!sr.EndOfStream)// 每次讀取一行，直到檔尾
                {
                    String line = sr.ReadLine();// 讀取文字到 line 變數

                    //--
                    //人員資料表全部(id和state除外)匯入
                    String Data = "";
                    if (intindex == 0)
                    {
                        StrTitle = line;
                        //---
                        //人員匯入/匯出CSV功能將[username,password,auth_group_id,del]欄位剔除
                        StrTitle += ",username,password";
                        //---人員匯入/匯出CSV功能將[username,password,auth_group_id,del]欄位剔除
                    }
                    else
                    {
                        if ((StrTitle.Length > 0) && (line.Length > 0))
                        {
                            //--
                            //車/人匯入時要有預設部門
                            string[] strs = line.Split(',');
                            String Strname, Stremp_no;
                            Strname = strs[0];//CSV順序
                            Stremp_no = strs[5];//人員匯入/匯出CSV功能將[username,password,auth_group_id,del]欄位剔除- strs[9];//CSV順序//工號
                            //--
                            
                            Data = "'" + line + "'";
                            Data = Data.Replace(",", "','");

                            //---
                            //人員匯入/匯出CSV功能將[username,password,auth_group_id,del]欄位剔除
                            String Strusername, Strpassword;
                            Strusername = Stremp_no;//工號
                            Strpassword = Web_encrypt.MD5_BASE64forPHP(Stremp_no);
                            Data += String.Format(",'{0}','{1}'", Strusername, Strpassword);
                            //---人員匯入/匯出CSV功能將[username,password,auth_group_id,del]欄位剔除

                            /*車/人匯入時要有預設部門
                            SQL += String.Format("INSERT INTO user ({0}) VALUES ({1});", StrTitle, Data);
                            */

                            //--
                            //車/人匯入時要有預設部門
                            m_intuser_id = -10;
                            //---

                            //---
                            //人員重複匯入防呆機制
                            //SQL = String.Format("INSERT INTO user ({0}) VALUES ({1});", StrTitle, Data);
                            //bool blnAns = MySQL.InsertUpdateDelete(SQL);//新增資料程式
                            bool blnAns = false;
                            SQL = String.Format("SELECT id FROM user WHERE emp_no='{1}';", Strname, Stremp_no);
                            MySqlDataReader Readercheck = MySQL.GetDataReader(SQL);//判斷資料是否已存在
                            if (!Readercheck.HasRows)
                            {
                                Readercheck.Close();
                                SQL = String.Format("INSERT INTO user ({0}) VALUES ({1});", StrTitle, Data);
                                blnAns = MySQL.InsertUpdateDelete(SQL);//新增資料程式
                            }
                            else
                            {
                                Readercheck.Close();
                                blnAns = false;
                            }
                            //---人員重複匯入防呆機制

                            if (blnAns == true)
                            {
                                SQL = String.Format("SELECT id FROM user WHERE name='{0}' AND emp_no='{1}';", Strname, Stremp_no);
                                MySqlDataReader DataReader = MySQL.GetDataReader(SQL);//新增資料
                                while (DataReader.Read())
                                {
                                    m_intuser_id = Convert.ToInt32(DataReader["id"].ToString());
                                }
                                DataReader.Close();
                                if (m_intuser_id > 0)
                                {
                                    SQL = String.Format("INSERT INTO department_detail (dep_id,state,user_id) VALUES ({0},{1},{2});", -1, 1, m_intuser_id);
                                    MySQL.InsertUpdateDelete(SQL);//新增資料
                                }
                            }
                            //--
                        }
                    }
                    //--

                    /*
                    //--
                    //2017/12/26之前的人員匯入
                    string[] strs = line.Split(',');
                    if ((strs.Length > 5) && (intindex>0))
                    {
                        Stremp_no = strs[0];
                        Strsecurity_id = strs[1];
                        Strname = strs[2];
                        Strdep_id = strs[3];
                        Strattribute = strs[4];
                        Strbirthday = strs[5];
                        if (Strdep_id == "")
                        {
                            Strdep_id = "-1";
                        }
                        SQL = String.Format("INSERT INTO user (emp_no, security_id, name,attribute,birthday,state) VALUES ('{0}', '{1}','{2}','{3}','{4}',1);", Stremp_no, Strsecurity_id, Strname, Strattribute, Strbirthday);
                        bool blnAns = MySQL.InsertUpdateDelete(SQL);//新增資料程式
                        if (blnAns == true)
                        {
                            SQL = String.Format("SELECT id FROM user WHERE name='{0}' AND security_id='{1}';", Strname, Strsecurity_id);
                            MySqlDataReader DataReader = MySQL.GetDataReader(SQL);//新增資料
                            while (DataReader.Read())
                            {
                                m_intuser_id = Convert.ToInt32(DataReader["id"].ToString());
                            }
                            DataReader.Close();
                            if (m_intuser_id > 0)
                            {
                                SQL = String.Format("INSERT INTO department_detail (dep_id,state,user_id) VALUES ({0},{1},{2});", Strdep_id, 1, m_intuser_id);
                                MySQL.InsertUpdateDelete(SQL);//新增資料
                                m_intuser_id = -10;
                            }
                        }
                    }
                    //--
                    */

                    intindex++;

                    /*車/人匯入時要有預設部門
                    //--
                    //人員資料表全部(id和state除外)匯入
                    if ((intindex == 50) && (SQL.Length > 0))
                    {
                        MySQL.InsertUpdateDelete(SQL);//新增資料
                        SQL = "";
                        intindex = 1;
                    }
                    //--
                    */

                }

                /*車/人匯入時要有預設部門
                //--
                //人員資料表全部(id和state除外)匯入
                if (SQL.Length > 0)
                {
                    MySQL.InsertUpdateDelete(SQL);//新增資料
                    SQL = "";
                }
                //--
                */ 

                sr.Close();// 關閉串流
                get_show_Users();//取得人員列表
            }
        }
        //---人員匯入改成執行緒模式

        private void butSub0100_04_Click(object sender, EventArgs e)//人員匯出
        {
            String StrPath = "";
            //String Stremp_no, Strsecurity_id, Strname, Strdep_id, Strattribute, Strbirthday;
            String SQL = "";
            SaveFileDialog saveFileDialog1 = new SaveFileDialog();
            saveFileDialog1.Filter = "CSV File|*.csv";
            saveFileDialog1.Title = "Save an CSV";
            saveFileDialog1.FileName = "user.csv";
            if (saveFileDialog1.ShowDialog() == DialogResult.OK)
            {
                StrPath = saveFileDialog1.FileName.ToString();
                StreamWriter sw = new StreamWriter(StrPath, false, System.Text.Encoding.UTF8);
                //--
                //取消車/人匯出圖片功能和無用欄位
                ArrayList delname = new ArrayList();
                //人員UICSV匯出/匯入功能修改-delname.Add("user_name");
                //人員UICSV匯出/匯入功能修改-delname.Add("password");
                delname.Add("status");
                delname.Add("employee_date");
                delname.Add("unemployee_date");
                delname.Add("pic");
                //---
                //人員匯入/匯出CSV功能將[username,password,auth_group_id,del]欄位剔除	
                delname.Add("username");
                delname.Add("password");
                delname.Add("auth_group_id");
                delname.Add("del");
                //---人員匯入/匯出CSV功能將[username,password,auth_group_id,del]欄位剔除	
                ArrayList delindex=new ArrayList();
                bool adddata = false;
                //--取消車/人匯出圖片功能和無用欄位
                //--
                //人員資料表全部(id和state除外)匯出
                //SQL = "SELECT * FROM user ORDER BY id ASC;";
                SQL = "SELECT d_d.dep_id,u.name, u.alias_name, u.gender, u.attribute, u.birthday, u.emp_no, u.security_id, u.passport_id, u.office_tel, u.home_tel, u.cell_phone, u.email, u.family_address, u.contact_address, u.emergency_contactor, u.emergency_tel, u.note FROM user AS u,department_detail AS d_d WHERE (d_d.user_id=u.id) AND (d_d.car_id IS NULL) ORDER BY u.id;";//『人員』匯出要有部門欄位
                String StrTitle = "";
                MySqlDataReader Reader_Data = MySQL.GetDataReader(SQL);
                for (int i = 0; i < (Reader_Data.VisibleFieldCount - 1); i++)
                {
                    //--
                    //取消車/人匯出圖片功能和無用欄位
                    bool delcheck = false;
                    for (int k = 0; k < delname.Count; k++)
                    {
                        if (Reader_Data.GetName(i) == delname[k].ToString())
                        {
                            delcheck = true;
                            break;
                        }
                    }
                    //--

                    if (!delcheck)//取消車/人匯出圖片功能和無用欄位
                    {
                        if (StrTitle.Length>0)
                        {
                            StrTitle += ",";
                        }
                        //---
                        //『人員』匯出要有部門欄位
                        StrTitle += Reader_Data.GetName(i);
                        /*
                        if (i > 0)
                        {
                            StrTitle += Reader_Data.GetName(i);
                        }
                        */
                        //---『人員』匯出要有部門欄位
                    }
                    else//取消車/人匯出圖片功能和無用欄位
                    {
                        delindex.Add(i);
                    }
                }
                sw.WriteLine(StrTitle);

                while (Reader_Data.Read())
                {
                    String Data = "";
                    for (int j = 0; j < (Reader_Data.VisibleFieldCount - 1); j++)
                    {
                        //--
                        //取消車/人匯出圖片功能和無用欄位
                        bool delcheck = false;
                        for (int l = 0; l < delindex.Count; l++)
                        {
                            if (Convert.ToInt32(delindex[l].ToString()) == j)
                            {
                                delcheck = true;
                                break;
                            }
                        }
                        //--
                        if (!delcheck)//取消車/人匯出圖片功能和無用欄位
                        {
                            if (adddata)
                            {
                                Data += ",";
                            }
                            //---
                            //『人員』匯出要有部門欄位
                            Data += Reader_Data[j].ToString();
                            adddata = true;
                            /*
                            if (j > 0)
                            {
                                Data += Reader_Data[j].ToString();
                                adddata = true;
                            }
                            */
                            //---『人員』匯出要有部門欄位
                        }
                    }
                    if (Data.Length > 0)
                    {
                        sw.WriteLine(Data);
                    }
                    adddata = false;//取消車/人匯出圖片功能和無用欄位
                }
                Reader_Data.Close();
                //--

                /*
                //--
                //2017/12/26之前版本的人員匯出功能
                sw.WriteLine("工號,身份證號,姓名,部門編號,職稱,生日");
                SQL = "SELECT emp_no,security_id,name,dep_id,attribute,birthday FROM user LEFT JOIN department_detail ON (user.id=department_detail.user_id) GROUP BY user.id ORDER BY user.id ASC;";
                MySqlDataReader Reader_Data = MySQL.GetDataReader(SQL);
                while (Reader_Data.Read())
                {
                    Stremp_no = Reader_Data["emp_no"].ToString();
                    Strsecurity_id = Reader_Data["security_id"].ToString();
                    Strname = Reader_Data["name"].ToString();
                    Strdep_id = Reader_Data["dep_id"].ToString();
                    Strattribute = Reader_Data["attribute"].ToString();
                    Strbirthday = Reader_Data["birthday"].ToString();
                    String Data = Stremp_no + "," + Strsecurity_id + "," + Strname + "," + Strdep_id + "," + Strattribute + "," + Strbirthday;
                    sw.WriteLine(Data);
                }
                Reader_Data.Close();
                //--
                */
                sw.Close();
            }
        }

        private void butSub0100_06_Click(object sender, EventArgs e)//人員全選
        {
            /*
            for (int i = 0; i < dgvSub0100_01.Rows.Count; i++)
            {
                dgvSub0100_01.Rows[i].Cells[0].Value = true;
                dgvSub0100_01.Rows[i].Selected = true;
            }
            */
            dgvSub0100_01.SelectAll();//把所有列表元件的全選/取消全選改成SelectAll()/ClearSelection()
        }

        private void butSub0100_07_Click(object sender, EventArgs e)//人員取消全選
        {
            /*
            for (int i = 0; i < dgvSub0100_01.Rows.Count; i++)
            {
                dgvSub0100_01.Rows[i].Cells[0].Value = false;
                dgvSub0100_01.Rows[i].Selected = false;
            }
            */
            dgvSub0100_01.ClearSelection();//把所有列表元件的全選/取消全選改成SelectAll()/ClearSelection()
        }

        private void butSub0100_08_Click(object sender, EventArgs e)//人員批次處理
        {
            int index = cmbSub0100_01.SelectedIndex;
            String SQL = "";
            ArrayList ALid = new ArrayList();
            ArrayList ALdname = new ArrayList();
            /*
            ArrayList ALjobnum = new ArrayList();
            ArrayList ALs_id = new ArrayList();
            ArrayList ALname = new ArrayList();
            ArrayList ALattribute = new ArrayList();
            ArrayList ALbirthday = new ArrayList();
            */ 
            for (int i = 0; i < dgvSub0100_01.Rows.Count; i++)
            {
                String data = dgvSub0100_01.Rows[i].Cells[0].Value.ToString().ToLower();//抓取DataGridView欄位資料
                if (data == "true")
                {
                    ALid.Add(dgvSub0100_01.Rows[i].Cells[1].Value.ToString());
                    ALdname.Add(dgvSub0100_01.Rows[i].Cells[5].Value.ToString());
                }
            }
            switch (index)
            {
                case 0://刪除
                    for (int i = 0; i < ALid.Count;i++)
                    {
                        int uid = Convert.ToInt32(ALid[i].ToString());
                        int depid = 0;
                        SQL = String.Format("SELECT id FROM department WHERE name='{0}';", ALdname[i].ToString());
                        MySqlDataReader DataReader = MySQL.GetDataReader(SQL);
                        while (DataReader.Read())
                        {
                            depid = Convert.ToInt32(DataReader["id"].ToString());
                            break;
                        }
                        DataReader.Close();
                        SQL = String.Format("DELETE FROM user WHERE id={0};DELETE FROM department_detail WHERE dep_id={1} AND user_id={0};", uid, depid);
                        MySQL.InsertUpdateDelete(SQL);//刪除
                    }
                    break;
            }
            get_show_Users();//取得人員列表
        }

        private void butSub0100_09_Click_XX(object sender, EventArgs e)//人員搜尋-單純UI文字搜尋
        {
            get_show_Users();//取得人員列表
            ArrayList AL01 = new ArrayList();
            ArrayList AL02 = new ArrayList();
            ArrayList AL03 = new ArrayList();
            ArrayList AL04 = new ArrayList();
            ArrayList AL05 = new ArrayList();
            ArrayList AL06 = new ArrayList();
            ArrayList AL07 = new ArrayList();
            if (txtSub0100_01.Text != "")
            {
                for (int i = 0; i < dgvSub0100_01.Rows.Count; i++)//取的現行UI上人員列表所有資料
                {
                    AL01.Add(dgvSub0100_01.Rows[i].Cells[1].Value.ToString());
                    AL02.Add(dgvSub0100_01.Rows[i].Cells[2].Value.ToString());
                    AL03.Add(dgvSub0100_01.Rows[i].Cells[3].Value.ToString());
                    AL04.Add(dgvSub0100_01.Rows[i].Cells[4].Value.ToString());
                    AL05.Add(dgvSub0100_01.Rows[i].Cells[5].Value.ToString());
                    AL06.Add(dgvSub0100_01.Rows[i].Cells[6].Value.ToString());
                    AL07.Add(dgvSub0100_01.Rows[i].Cells[7].Value.ToString());
                }

                cleandgvSub0100_01();//清空畫面
                
                String StrSearch = txtSub0100_01.Text;
                for (int i = 0; i < AL01.Count; i++)
                {
                    if ((AL02[i].ToString().IndexOf(StrSearch) > -1) || (AL03[i].ToString().IndexOf(StrSearch) > -1) || (AL04[i].ToString().IndexOf(StrSearch) > -1) || (AL05[i].ToString().IndexOf(StrSearch) > -1) || (AL06[i].ToString().IndexOf(StrSearch) > -1) || (AL07[i].ToString().IndexOf(StrSearch) > -1))
                    {
                        dgvSub0100_01.Rows.Add(false, AL01[i].ToString(), AL02[i].ToString(), AL03[i].ToString(), AL04[i].ToString(), AL05[i].ToString(), AL06[i].ToString(), AL07[i].ToString());
                    }
                }
            }
        }

        public String m_SQL_user_condition01 = "";
        private void butSub0100_09_Click(object sender, EventArgs e)//人員搜尋改成DB語法
        {
            m_SQL_user_condition01 = "";
            m_intUserNowPage = 1;
            if (txtSub0100_01.Text != "")
            {
                m_SQL_user_condition01 = String.Format("AND ( (u.emp_no LIKE '%{0}%') OR (u.name LIKE '%{0}%') OR (d.name LIKE '%{0}%') OR (u.attribute LIKE '%{0}%') OR(u.birthday LIKE '%{0}%') )", txtSub0100_01.Text);
                
            }
            get_show_Users();
        }

        private void dgvSub0100_01_SelectionChanged(object sender, EventArgs e)//人員列表選擇
        {
            //---
            //列表元件shift全選時最前面的狀態要能自動打勾，反之要能取消
            for (int i = 0; i < dgvSub0100_01.Rows.Count; i++)
            {
                dgvSub0100_01.Rows[i].Cells[0].Value = false;
            }
            for (int j = 0; j < dgvSub0100_01.SelectedRows.Count; j++)
            {
                dgvSub0100_01.SelectedRows[j].Cells[0].Value = true;
            }
            //---列表元件shift全選時最前面的狀態要能自動打勾，反之要能取消

            try
            {
                String SQL;
                int index = dgvSub0100_01.SelectedRows[0].Index;//取得被選取的第一列位置
                String Struser_id = dgvSub0100_01.Rows[index].Cells[1].Value.ToString();
                m_intuser_id = Int32.Parse(Struser_id);
                SQL = String.Format("SELECT id FROM department WHERE name='{0}';", dgvSub0100_01.Rows[index].Cells[5].Value.ToString());
                MySqlDataReader DataReader = MySQL.GetDataReader(SQL);
                while (DataReader.Read())
                {
                    m_intdep_id = Convert.ToInt32(DataReader["id"].ToString());
                    break;
                }
                DataReader.Close();
            }
            catch
            {
            }
        }

        private void butSub0100_12_Click(object sender, EventArgs e)//人員列表移至第一頁
        {
            m_intUserNowPage = 1;
            get_show_Users();
        }

        private void butSub0100_13_Click(object sender, EventArgs e)//人員列表移至前一頁
        {
            m_intUserNowPage--;
            if (m_intUserNowPage < 1)
            {
                m_intUserNowPage = 1;
            }
            get_show_Users();
        }

        private void butSub0100_14_Click(object sender, EventArgs e)//人員列表移至後一頁
        {
            m_intUserNowPage++;
            if (m_intUserNowPage > m_intUserAllPage)
            {
                m_intUserNowPage = m_intUserAllPage;
            }
            get_show_Users();

        }

        private void butSub0100_15_Click(object sender, EventArgs e)//人員列表移至最後一頁
        {
            m_intUserNowPage = m_intUserAllPage;
            get_show_Users();
        }
        //Sub0100_end
        //Sub010000_start
        private void dgvSub010000_01_DoubleClick(object sender, EventArgs e)//at 2017/09/15
        {
            butSub010000_08.PerformClick();
        }

        public bool UserData2DB(bool blnRunSQL=true,int intState = 1)
        {
            bool blnAns = false;
            String SQL = "";
            String Strname, Stralias_name, Strgender, Strattribute, Strbirthday, Stremp_no, Strsecurity_id, Strpassport_id, Stroffice_tel, Strhome_tel, Strcell_phone, Stremergency_contactor, Stremail, Stremergency_tel, Strfamily_address, Strcontact_address, Strnote;
            String Strdep_id = "-2";
            String StrImageData = FileLib.ImageFile2Base64String(FileLib.path + "\\temp.png");
            String Strusername, Strpassword;//人員UI增加DB填入帳密功能
            String Strstatus;//人機介面要能設定是否能登錄 ~ DB關聯(寫入時)

            //---
            //人機介面要能設定是否能登錄 ~ DB關聯(寫入時)
            Strstatus = "0";
            if (ckbSub010000_01.Checked==true)
            {
                Strstatus="1";
            }
            else
            {
                Strstatus="0";
            }
            //---人機介面要能設定是否能登錄 ~ DB關聯(寫入時)

            Strname = txtSub010000_01.Text;

            Stralias_name = txtSub010000_02.Text;

            if (rdbSub010000_01.Checked == true)
            {
                Strgender = "1";
            }
            else
            {
                Strgender = "0";
            }

            Strattribute = txtSub010000_04.Text;

            Strbirthday = txtSub010000_05.Value.ToString("yyyy-MM-dd");

            Stremp_no = txtSub010000_06.Text;

            Strsecurity_id = txtSub010000_07.Text;

            Strpassport_id = txtSub010000_08.Text;

            Stroffice_tel = txtSub010000_09.Text;

            Strhome_tel = txtSub010000_10.Text;

            Strcell_phone = txtSub010000_11.Text;

            Stremergency_contactor = txtSub010000_12.Text;

            Stremail = txtSub010000_13.Text;

            Stremergency_tel = txtSub010000_14.Text;

            Strfamily_address = txtSub010000_15.Text;

            Strcontact_address = txtSub010000_16.Text;

            Strnote = txtSub010000_17.Text;

            if (cmbSub010000_02.SelectedIndex >= 0)
            {
                Strdep_id = m_ALDepartment_ID[cmbSub010000_02.SelectedIndex].ToString();
            }

            //--
            //add at 2017/10/11
            if (!blnRunSQL)
            {
                m_Sub010000ALData.Clear();
                m_Sub010000ALData.Add(txtSub010000_01.Text);
                m_Sub010000ALData.Add(txtSub010000_02.Text);
                m_Sub010000ALData.Add(rdbSub010000_01.Checked.ToString());
                m_Sub010000ALData.Add(rdbSub010000_02.Checked.ToString());
                m_Sub010000ALData.Add(cmbSub010000_02.SelectedIndex + "");
                m_Sub010000ALData.Add(txtSub010000_04.Text);
                m_Sub010000ALData.Add(txtSub010000_05.Value.ToString("yyyy-MM-dd HH:mm"));
                m_Sub010000ALData.Add(txtSub010000_06.Text);
                m_Sub010000ALData.Add(txtSub010000_07.Text);
                m_Sub010000ALData.Add(txtSub010000_08.Text);
                m_Sub010000ALData.Add(txtSub010000_09.Text);
                m_Sub010000ALData.Add(txtSub010000_10.Text);
                m_Sub010000ALData.Add(txtSub010000_11.Text);
                m_Sub010000ALData.Add(txtSub010000_12.Text);
                m_Sub010000ALData.Add(txtSub010000_13.Text);
                m_Sub010000ALData.Add(txtSub010000_14.Text);
                m_Sub010000ALData.Add(txtSub010000_15.Text);
                m_Sub010000ALData.Add(txtSub010000_16.Text);
                m_Sub010000ALData.Add(txtSub010000_17.Text);
                m_Sub010000ALData.Add(ckbSub010000_01.Checked.ToString());//人機介面要能設定是否能登錄 ~ UI變化紀錄

                if (StrImageData.Length > 0)
                {
                    m_Sub010000ALData.Add(StrImageData);
                }

                for (int i = 0; i < dgvSub010000_01.Rows.Count; i++)
                {
                    m_Sub010000ALData.Add(dgvSub010000_01.Rows[i].Cells[1].Value.ToString());
                }

                return (!blnRunSQL);
            }
            //--

            if ((Strdep_id != "-2") && (Strname != "") && (Stremp_no != ""))//人員UI移除身分證號為必填欄位判斷機制- if ((Strdep_id != "-2") && (Strname != "") && (Strsecurity_id != "") && (Stremp_no != ""))
            {
                labSub010000_01.ForeColor = Color.Black;
                labSub010000_05.ForeColor = Color.Black;
                labSub010000_09.ForeColor = Color.Black;
                labSub010000_08.ForeColor = Color.Black;//人員工號改必填-增加必填防呆偵測
            }
            else
            {
                if (Strdep_id != "-1")
                {
                    labSub010000_05.ForeColor = Color.Black;
                }
                else
                {
                    labSub010000_05.ForeColor = Color.Red;
                }
                if (Strname != "")
                {
                    labSub010000_01.ForeColor = Color.Black;
                }
                else
                {
                    labSub010000_01.ForeColor = Color.Red;
                }

                /*人員UI移除身分證號為必填欄位判斷機制
                if (Strsecurity_id != "")
                {
                    labSub010000_09.ForeColor = Color.Black;
                }
                else
                {
                    labSub010000_09.ForeColor = Color.Red;
                }
                */

                //---
                //人員工號改必填-增加必填防呆偵測
                if (Stremp_no != "")
                {
                    labSub010000_08.ForeColor = Color.Black;
                }
                else
                {
                    labSub010000_08.ForeColor = Color.Red;
                }
                //---人員工號改必填-增加必填防呆偵測

                blnAns = false;
                return blnAns;
            }

            //---
            //人員UI增加DB填入帳密功能
            Strusername = Stremp_no;
            Strpassword = Web_encrypt.MD5_BASE64forPHP(Stremp_no);
            //---人員UI增加DB填入帳密功能

            if (m_intuser_id > 0)//修改
            {
                //---
                //人員UI增加DB填入帳密功能
                /*
                SQL = String.Format("UPDATE user SET name='{0}',alias_name='{1}',gender='{2}',attribute='{3}',birthday='{4}',emp_no='{5}',security_id='{6}',passport_id='{7}',office_tel='{8}',home_tel='{9}',cell_phone='{10}',emergency_contactor='{11}',email='{12}',emergency_tel='{13}',family_address='{14}',contact_address='{15}',note='{16}',state={17},pic='{18}' WHERE id={19};",
                                                      Strname, Stralias_name, Strgender, Strattribute, Strbirthday, Stremp_no, Strsecurity_id, Strpassport_id, Stroffice_tel, Strhome_tel, Strcell_phone, Stremergency_contactor, Stremail, Stremergency_tel, Strfamily_address, Strcontact_address, Strnote, intState, StrImageData, m_intuser_id);
                */
                SQL = String.Format("UPDATE user SET name='{0}',alias_name='{1}',gender='{2}',attribute='{3}',birthday='{4}',emp_no='{5}',security_id='{6}',passport_id='{7}',office_tel='{8}',home_tel='{9}',cell_phone='{10}',emergency_contactor='{11}',email='{12}',emergency_tel='{13}',family_address='{14}',contact_address='{15}',note='{16}',state={17},pic='{18}',username='{20}',password='{21}',status='{22}' WHERE id={19};",
                                      Strname, Stralias_name, Strgender, Strattribute, Strbirthday, Stremp_no, Strsecurity_id, Strpassport_id, Stroffice_tel, Strhome_tel, Strcell_phone, Stremergency_contactor, Stremail, Stremergency_tel, Strfamily_address, Strcontact_address, Strnote, intState, StrImageData, m_intuser_id, Strusername, Strpassword, Strstatus);//人機介面要能設定是否能登錄 ~ DB關聯(寫入時)
                //---人員UI增加DB填入帳密功能

                if (Strdep_id != "-2")
                {
                    //SQL += String.Format("UPDATE department_detail SET dep_id={0},state={1} WHERE user_id={2};", Strdep_id, intState, m_intuser_id);
                    
                    //--
                    //人員資料表全部(id和state除外)匯入
                    bool check = false;
                    MySqlDataReader Reader_Data = MySQL.GetDataReader(String.Format("SELECT * FROM department_detail WHERE (car_id IS NULL) AND user_id={0};", m_intuser_id));
                    while (Reader_Data.Read())
                    {
                        check = true;
                    }
                    Reader_Data.Close();
                    if (check == true)
                    {
                        SQL += String.Format("UPDATE department_detail SET dep_id={0},state={1} WHERE (car_id IS NULL) AND user_id={2};", Strdep_id, intState, m_intuser_id);
                    }
                    else
                    {
                        SQL += String.Format("INSERT INTO department_detail (dep_id,state,user_id) VALUES ({0},{1},{2});", Strdep_id, intState, m_intuser_id);
                    }
                    //--

                }
                blnAns = MySQL.InsertUpdateDelete(SQL);//更新資料
            }
            else//新增
            {
                //---
                //人員UI增加DB填入帳密功能
                /*
                SQL = String.Format("INSERT INTO user (name, alias_name, gender, attribute, birthday, emp_no, security_id, passport_id, office_tel, home_tel, cell_phone, emergency_contactor, email, emergency_tel, family_address, contact_address, note,pic,state) VALUES ('{0}', '{1}','{2}','{3}','{4}','{5}','{6}','{7}','{8}','{9}','{10}','{11}','{12}','{13}','{14}','{15}','{16}','{17}',{18});"
                                                  , Strname, Stralias_name, Strgender, Strattribute, Strbirthday, Stremp_no, Strsecurity_id, Strpassport_id, Stroffice_tel, Strhome_tel, Strcell_phone, Stremergency_contactor, Stremail, Stremergency_tel, Strfamily_address, Strcontact_address, Strnote, StrImageData, intState);
                */

                //---
                //人員UI工號欄位判斷不能重複
                SQL =String.Format("SELECT id FROM user WHERE username='{0}';",Strusername);
                MySqlDataReader ReaderCheck = MySQL.GetDataReader(SQL);
                if (ReaderCheck.HasRows)
                {
                    txtSub010000_06.Text = "";
                    labSub010000_08.ForeColor = Color.Red;
                    ReaderCheck.Close();
                    blnAns = false;
                    return blnAns;
                }
                ReaderCheck.Close();
                //---人員UI工號欄位判斷不能重複

                SQL = String.Format("INSERT INTO user (name, alias_name, gender, attribute, birthday, emp_no, security_id, passport_id, office_tel, home_tel, cell_phone, emergency_contactor, email, emergency_tel, family_address, contact_address, note,pic,state, username, password, status) VALUES ('{0}', '{1}','{2}','{3}','{4}','{5}','{6}','{7}','{8}','{9}','{10}','{11}','{12}','{13}','{14}','{15}','{16}','{17}',{18},'{19}','{20}','{21}');"
                                                  , Strname, Stralias_name, Strgender, Strattribute, Strbirthday, Stremp_no, Strsecurity_id, Strpassport_id, Stroffice_tel, Strhome_tel, Strcell_phone, Stremergency_contactor, Stremail, Stremergency_tel, Strfamily_address, Strcontact_address, Strnote, StrImageData, intState, Strusername, Strpassword, Strstatus);//人機介面要能設定是否能登錄 ~ DB關聯(寫入時)
                //---人員UI增加DB填入帳密功能
                blnAns = MySQL.InsertUpdateDelete(SQL);
                if (blnAns == true)
                {
                    SQL = String.Format("SELECT id FROM user WHERE name='{0}' AND security_id='{1}';", Strname, Strsecurity_id);
                    MySqlDataReader DataReader = MySQL.GetDataReader(SQL);//新增資料
                    while (DataReader.Read())
                    {
                        m_intuser_id = Convert.ToInt32(DataReader["id"].ToString());
                    }
                    DataReader.Close();
                    if (m_intuser_id > 0)
                    {
                        SQL = String.Format("UPDATE card_for_user_car SET user_id={0} WHERE user_id=-10;", m_intuser_id);
                        MySQL.InsertUpdateDelete(SQL);
                        SQL = String.Format("INSERT INTO department_detail (dep_id,state,user_id) VALUES ({0},{1},{2});", Strdep_id, intState, m_intuser_id);
                        blnAns = MySQL.InsertUpdateDelete(SQL);//新增資料
                    }
                }
            }

            if (blnAns)
            {
                m_intuser_id = -1;
            }

            return blnAns;
        }

        private void butSub010000_13_Click(object sender, EventArgs e)//儲存人員資料
        {
            if (UserData2DB())//儲存人員資料成功
            {
                get_show_Users();//取得人員列表
                Leave_function();
            }
        }

        private void butSub010000_01_Click(object sender, EventArgs e)//清除圖片
        {
            imgSub010000_01.Image = null;
            FileLib.DeleteFile("temp.png");
        }

        private void butSub010000_02_Click(object sender, EventArgs e)//載入圖片
        {
            String StrPath;
            OpenFileDialog openFileDialog1 = new OpenFileDialog();
            openFileDialog1.Filter = "Image File|*.png;*.jpg";
            openFileDialog1.Title = "Open an Image";
            openFileDialog1.RestoreDirectory = true;
            if (openFileDialog1.ShowDialog() == DialogResult.OK)
            {
                imgSub010000_01.Image = null;
                StrPath = openFileDialog1.FileName.ToString();
                String StrDestFilePath = FileLib.path;
                FileLib.DeleteFile("temp.png");
                /*
                if (StrPath.IndexOf(".png") >= 0)
                {
                    StrDestFilePath += "\\" + "temp.png";
                }
                else
                {
                    StrDestFilePath += "\\" + "temp.jpg";
                }
                */
                //---
                //整合縮圖函數
                //StrDestFilePath += "\\" + "temp.png";
                //FileLib.CopyFile(StrPath, StrDestFilePath);
                StrDestFilePath += "\\" + "temp.png";
                FileLib.ImageResize(StrPath, StrDestFilePath,800);
                //---整合縮圖函數

                //--
                //c# 圖片檔讀取：非鎖定檔方法~http://fecbob.pixnet.net/blog/post/38125005
                FileStream fs = File.OpenRead(StrDestFilePath); //OpenRead
                int filelength = 0;
                filelength = (int)fs.Length; //獲得檔長度
                Byte[] image = new Byte[filelength]; //建立一個位元組陣列
                fs.Read(image, 0, filelength); //按位元組流讀取
                System.Drawing.Image result = System.Drawing.Image.FromStream(fs);
                fs.Close();
                //--

                imgSub010000_01.Image = result;//Image.FromFile(StrDestFilePath);
            }
        }

        public ArrayList m_ALCardList = new ArrayList();
        private void butSub010000_04_Click(object sender, EventArgs e)//配發卡片
        {
            String SQL = "";
            m_ALCardList.Clear();
            CardList frmCL = new CardList(this);
            frmCL.ShowDialog();
            for (int i = 0; i < m_ALCardList.Count; i++)
            {
                String StrCard_id = m_ALCardList[i].ToString();
                SQL = String.Format("INSERT INTO card_for_user_car (card_id,user_id,status,state) VALUES ({0},{1},1,1);", StrCard_id, m_intuser_id);
                MySQL.InsertUpdateDelete(SQL);
            }
            get_show_UserCards(m_intuser_id);//取得人員的卡片列表
        }

        private void butSub010000_15_Click(object sender, EventArgs e)//離開
        {
            //--
            //add at 2017/10/11
            UserData2DB(false);
            labSub010000_01.ForeColor = Color.Black;
            labSub010000_05.ForeColor = Color.Black;
            labSub010000_09.ForeColor = Color.Black;
            labSub010000_08.ForeColor = Color.Black;//人員工號改必填-增加必填防呆偵測
            //--

            if ((m_intuser_id == -1) || CheckUIVarNotChange(m_Sub010000ALInit, m_Sub010000ALData))//if (m_intuser_id == -1)
            {
                Leave_function();
                get_show_Users();//取得人員列表
            }
            else
            {
                DialogResult myResult = MessageBox.Show(Language.m_StrControllerMsg00, butSub010000_15.Text, MessageBoxButtons.YesNo, MessageBoxIcon.Question);
                if (myResult == DialogResult.Yes)
                {
                    //--
                    //add at 2017/10/18
                    String SQL = "";
                    String StrCard_id = "";
                    if (m_intuser_id>0)
                    {
                        SQL = String.Format("DELETE FROM card_for_user_car WHERE user_id={0};", m_intuser_id);
                        MySQL.InsertUpdateDelete(SQL);
                        for (int i = 0; i < m_Sub010000ALRight.Count; i++)
                        {
                            StrCard_id = m_Sub010000ALRight[i].ToString();
                            SQL = String.Format("INSERT INTO card_for_user_car (card_id,user_id,status,state) VALUES ({0},{1},1,1);", StrCard_id, m_intuser_id);
                            MySQL.InsertUpdateDelete(SQL);
                        }
                    }
                    //--
                    Leave_function();
                    get_show_Users();//取得人員列表
                }
            }
        }

        private void butSub010000_05_Click(object sender, EventArgs e)//人員編輯UI中的卡片全選
        {
            /*
            for (int i = 0; i < dgvSub010000_01.Rows.Count; i++)
            {
                dgvSub010000_01.Rows[i].Cells[0].Value = true;
                dgvSub010000_01.Rows[i].Selected = true;
            }
            */
            dgvSub010000_01.SelectAll();//把所有列表元件的全選/取消全選改成SelectAll()/ClearSelection()
        }

        private void butSub010000_06_Click(object sender, EventArgs e)//人員編輯UI中的卡片取消全選
        {
            /*
            for (int i = 0; i < dgvSub010000_01.Rows.Count; i++)
            {
                dgvSub010000_01.Rows[i].Cells[0].Value = false;
                dgvSub010000_01.Rows[i].Selected = false;
            }
            */
            dgvSub010000_01.ClearSelection();//把所有列表元件的全選/取消全選改成SelectAll()/ClearSelection()
        }

        private void butSub010000_07_Click(object sender, EventArgs e)//人員編輯UI中的卡片批次處理
        {
            String SQL = "";
            ArrayList ALcard_id = new ArrayList();
            ALcard_id.Clear();
            for (int i = 0; i < dgvSub010000_01.Rows.Count; i++)
            {
                String data = dgvSub010000_01.Rows[i].Cells[0].Value.ToString().ToLower();
                if (data == "true")
                {
                    ALcard_id.Add(dgvSub010000_01.Rows[i].Cells[1].Value.ToString());
                }
            }

            switch (cmbSub010000_01.SelectedIndex)
            {
                case 0:
                    for (int i = 0; i < ALcard_id.Count; i++)
                    {
                        SQL = String.Format("DELETE FROM card_for_user_car WHERE card_id={0} AND user_id={1};", ALcard_id[i].ToString(), m_intuser_id);
                        MySQL.InsertUpdateDelete(SQL);
                    }
                    break;
            }

            get_show_UserCards(m_intuser_id);//取得人員的卡片列表
        }

        private void butSub010000_08_Click(object sender, EventArgs e)//人員編輯UI中的編輯卡片
        {
            m_intcard_id = -1;

            try
            {
                int index = dgvSub010000_01.SelectedRows[0].Index;//取得被選取的第一列位置
                String Strcard_id = dgvSub010000_01.Rows[index].Cells[1].Value.ToString();
                m_intcard_id = Int32.Parse(Strcard_id);
            }
            catch
            {
            }

            modifiedCardData();
        }

        public int m_intUserAddCard_id = 0;//add 2017/11/01
        private void butSub010000_16_Click(object sender, EventArgs e)//人員新增時，呼叫卡片新增
        {
            //--
            //add 2017/11/01
            m_TPOld = m_tabMain.SelectedTab;//--2017/02/22 製作返回按鈕功能
            TabPage_Push();//m_StackTPOld.Push(m_TPOld);//--2017/02/22 製作返回按鈕功能

            m_tabSub010200.Parent = m_tabMain;
            labSub010200_08.ReadOnly = false;//可編輯卡片內碼
            m_intcard_id = -10;
            initSub010200UI(txtSub010000_01.Text);//人和車輛UI在立即配發卡片時利用程式手法直接顯示對應持有人名稱(原本要DB有資料才關聯出來) initSub010200UI();
            m_tabMain.SelectedTab = m_tabSub010200;

            m_Sub010200ALInit.Clear();
            m_Sub010200ALInit.Add(labSub010200_07.Text);
            m_Sub010200ALInit.Add(labSub010200_08.Text);
            m_Sub010200ALInit.Add(txtSub010200_01.Text);
            m_Sub010200ALInit.Add(txtSub010200_02.Text);
            m_Sub010200ALInit.Add(txtSub010200_03.Text);
            m_Sub010200ALInit.Add(cmbSub010200_01.SelectedIndex + "");
            m_Sub010200ALInit.Add(adpSub010200_01.Value.ToString("yyyy-MM-dd HH:mm"));
            m_Sub010200ALInit.Add(adpSub010200_02.Value.ToString("yyyy-MM-dd HH:mm"));
            m_Sub010200ALInit.Add(ckbSub010200_01.Checked.ToString());
            m_Sub010200ALInit.Add(ckbSub010200_02.Checked.ToString());
            m_Sub010200ALInit.Add(ckbSub010200_03.Checked.ToString());
            m_Sub010200ALInit.Add(ckbSub010200_04.Checked.ToString());
            m_Sub010200ALInit.Add(ckbSub010200_05.Checked.ToString());
            m_Sub010200ALInit.Add(ckbSub010200_06.Checked.ToString());
            m_Sub010200ALInit.Add(ckbSub010200_07.Checked.ToString());
            m_Sub010200ALInit.Add(ckbSub010200_08.Checked.ToString());
            m_Sub010200ALInit.Add(ckbSub010200_09.Checked.ToString());
            m_Sub010200ALInit.Add(ckbSub010200_10.Checked.ToString());
            m_Sub010200ALInit.Add(ckbSub010200_11.Checked.ToString());
            m_Sub010200ALInit.Add(rdbSub010200_01.Checked.ToString());
            m_Sub010200ALInit.Add(rdbSub010200_02.Checked.ToString());
            m_Sub010200ALInit.Add(rdbSub010200_03.Checked.ToString());
            m_Sub010200ALInit.Add(rdbSub010200_04.Checked.ToString());
            m_Sub010200ALInit.Add(steSub010200_01.StrValue1 + steSub010200_01.StrValue2);
            m_Sub010200ALInit.Add(steSub010200_02.StrValue1 + steSub010200_02.StrValue2);
            m_Sub010200ALInit.Add(steSub010200_03.StrValue1 + steSub010200_03.StrValue2);

            labSub010200_08.Focus();
            m_intUserAddCard_id = 0;
            m_intUserAddCard_id = m_intuser_id;
            //--
        }

        private void dgvSub010000_01_SelectionChanged(object sender, EventArgs e)
        {
            //---
            //列表元件shift全選時最前面的狀態要能自動打勾，反之要能取消
            for (int i = 0; i < dgvSub010000_01.Rows.Count; i++)
            {
                dgvSub010000_01.Rows[i].Cells[0].Value = false;
            }
            for (int j = 0; j < dgvSub010000_01.SelectedRows.Count; j++)
            {
                dgvSub010000_01.SelectedRows[j].Cells[0].Value = true;
            }
            //---列表元件shift全選時最前面的狀態要能自動打勾，反之要能取消
        }

        private void butSub010000_03_Click(object sender, EventArgs e)//二代證讀取按鈕
        {
            MessageBox.Show("Sorry, the feature is not implemented", "Information", MessageBoxButtons.OK, MessageBoxIcon.Information);
        }
        
        //Sub010000_end

        //Sub0102_start
        private void dgvSub0102_01_DoubleClick(object sender, EventArgs e)//at 2017/09/15
        {
            butSub0102_01.PerformClick();
        }
        public int m_intcard_id = -1;//紀錄card_id
        public void modifiedCardData()
        {
            if (m_intcard_id > 0)
            {
                String SQL = "";
                m_TPOld = m_tabMain.SelectedTab;//--2017/02/22 製作返回按鈕功能
                TabPage_Push();//m_StackTPOld.Push(m_TPOld);//--2017/02/22 製作返回按鈕功能

                m_tabSub010200.Parent = m_tabMain;
                initSub010200UI();
                labSub010200_08.ReadOnly = true;//不可編輯卡片內碼
                //SQL = String.Format("SELECT c.id AS id,u.name AS name,c.card_code AS card_code,c.alias AS alias,c.pin AS pin,c.display AS display,c.type AS type,c.available_date_start AS available_date_start,c.available_date_end AS available_date_end,c.active AS active,c.block AS block,c.apb_enable AS apb_enable,c.level AS level,c.week_plan AS week_plan,c.access_time_1_start AS access_time_1_start,c.access_time_2_start AS access_time_2_start,c.access_time_3_start AS access_time_3_start,c.access_time_1_end AS access_time_1_end,c.access_time_2_end AS access_time_2_end,c.access_time_3_end AS access_time_3_end FROM card AS c LEFT JOIN card_for_user_car AS cfuc ON c.id=cfuc.card_id LEFT JOIN card_type AS ct ON c.type=ct.id LEFT JOIN user AS u ON u.id=cfuc.user_id WHERE c.id={0};", m_intcard_id);
                SQL = String.Format("SELECT c.id AS id,u.name AS name01,car.name AS name02,c.card_code AS card_code,c.alias AS alias,c.pin AS pin,c.display AS display,c.type AS type,c.available_date_start AS available_date_start,c.available_date_end AS available_date_end,c.active AS active,c.block AS block,c.apb_enable AS apb_enable,c.level AS level,c.week_plan AS week_plan,c.access_time_1_start AS access_time_1_start,c.access_time_2_start AS access_time_2_start,c.access_time_3_start AS access_time_3_start,c.access_time_1_end AS access_time_1_end,c.access_time_2_end AS access_time_2_end,c.access_time_3_end AS access_time_3_end FROM card AS c LEFT JOIN card_for_user_car AS cfuc ON c.id=cfuc.card_id LEFT JOIN card_type AS ct ON c.type=ct.id LEFT JOIN user AS u ON u.id=cfuc.user_id LEFT JOIN car AS car ON car.id=cfuc.car_id WHERE c.id={0};", m_intcard_id);
                MySqlDataReader Reader_Data = MySQL.GetDataReader(SQL);
                while (Reader_Data.Read())
                {
                    //Reader_Data["id"].ToString();
                    labSub010200_07.Text = Reader_Data["name01"].ToString()+ Reader_Data["name02"].ToString();//使用者名稱

                    labSub010200_08.Text = Reader_Data["card_code"].ToString();//卡片內碼

                    txtSub010200_01.Text = Reader_Data["alias"].ToString();//別名

                    txtSub010200_02.StrValue = Reader_Data["pin"].ToString();//卡片密碼

                    txtSub010200_03.Text = Reader_Data["display"].ToString();//顯示文字

                    for (int i = 0; i < m_ALCardType_ID.Count; i++)
                    {
                        if (Reader_Data["type"].ToString() == m_ALCardType_ID[i].ToString())//卡片類型
                        {
                            cmbSub010200_01.SelectedIndex = i;
                        }
                    }

                    try
                    {
                        adpSub010200_01.Value = Convert.ToDateTime(Reader_Data["available_date_start"].ToString());//效期
                        adpSub010200_02.Value = Convert.ToDateTime(Reader_Data["available_date_end"].ToString());
                    }
                    catch
                    {
                        adpSub010200_01.Value = DateTime.Now;
                        adpSub010200_02.Value = DateTime.Now;
                    }

                    if (Reader_Data["active"].ToString() != "0")//停用與否
                    {
                        ckbSub010200_01.Checked = false;
                    }
                    else
                    {
                        ckbSub010200_01.Checked = true;
                    }

                    if (Reader_Data["block"].ToString() == "0")//黑名單
                    {
                        ckbSub010200_02.Checked = false;
                    }
                    else
                    {
                        ckbSub010200_02.Checked = true;
                    }

                    if (Reader_Data["apb_enable"].ToString() != "0")//APB啟用
                    {
                        ckbSub010200_03.Checked = false;
                    }
                    else
                    {
                        ckbSub010200_03.Checked = true;
                    }

                    switch (Convert.ToInt32(Reader_Data["level"].ToString()))//通行等級
                    {
                        case 0:
                            rdbSub010200_01.Checked = true;
                            break;
                        case 1:
                            rdbSub010200_02.Checked = true;
                            break;
                        case 2:
                            rdbSub010200_03.Checked = true;
                            break;
                        case 3:
                            rdbSub010200_04.Checked = true;
                            break;
                    }

                    int week_plan = Convert.ToInt32(Reader_Data["week_plan"].ToString(), 2);//周計畫[二進位字串 轉 十進位整數]
                    ckbSub010200_04.Checked = Convert.ToBoolean((week_plan & 1));
                    ckbSub010200_05.Checked = Convert.ToBoolean((week_plan & 2));
                    ckbSub010200_06.Checked = Convert.ToBoolean((week_plan & 4));
                    ckbSub010200_07.Checked = Convert.ToBoolean((week_plan & 8));
                    ckbSub010200_08.Checked = Convert.ToBoolean((week_plan & 16));
                    ckbSub010200_09.Checked = Convert.ToBoolean((week_plan & 32));
                    ckbSub010200_10.Checked = Convert.ToBoolean((week_plan & 64));
                    ckbSub010200_11.Checked = Convert.ToBoolean((week_plan & 128));

                    String StrBuf;
                    steSub010200_01.blnEnable = true;
                    steSub010200_02.blnEnable = true;
                    steSub010200_03.blnEnable = true;

                    StrBuf = Reader_Data["access_time_1_start"].ToString();
                    string[] strs01 = StrBuf.Split(':');
                    steSub010200_01.StrValue1 = strs01[0] + ":" + strs01[1];

                    StrBuf = Reader_Data["access_time_1_end"].ToString();
                    string[] strs02 = StrBuf.Split(':');
                    steSub010200_01.StrValue2 = strs02[0] + ":" + strs02[1];

                    StrBuf = Reader_Data["access_time_2_start"].ToString();
                    string[] strs03 = StrBuf.Split(':');
                    steSub010200_02.StrValue1 = strs03[0] + ":" + strs03[1];

                    StrBuf = Reader_Data["access_time_2_end"].ToString();
                    string[] strs04 = StrBuf.Split(':');
                    steSub010200_02.StrValue2 = strs04[0] + ":" + strs04[1];

                    StrBuf = Reader_Data["access_time_3_start"].ToString();
                    string[] strs05 = StrBuf.Split(':');
                    steSub010200_03.StrValue1 = strs05[0] + ":" + strs05[1];

                    StrBuf = Reader_Data["access_time_3_end"].ToString();
                    string[] strs06 = StrBuf.Split(':');
                    steSub010200_03.StrValue2 = strs06[0] + ":" + strs06[1];
                }
                Reader_Data.Close();

                m_tabMain.SelectedTab = m_tabSub010200;

                //--
                //add at 2017/10/12
                m_Sub010200ALInit.Clear();
                m_Sub010200ALInit.Add(labSub010200_07.Text);
                m_Sub010200ALInit.Add(labSub010200_08.Text);
                m_Sub010200ALInit.Add(txtSub010200_01.Text);
                m_Sub010200ALInit.Add(txtSub010200_02.Text);
                m_Sub010200ALInit.Add(txtSub010200_03.Text);
                m_Sub010200ALInit.Add(cmbSub010200_01.SelectedIndex + "");
                m_Sub010200ALInit.Add(adpSub010200_01.Value.ToString("yyyy-MM-dd HH:mm"));
                m_Sub010200ALInit.Add(adpSub010200_02.Value.ToString("yyyy-MM-dd HH:mm"));
                m_Sub010200ALInit.Add(ckbSub010200_01.Checked.ToString());
                m_Sub010200ALInit.Add(ckbSub010200_02.Checked.ToString());
                m_Sub010200ALInit.Add(ckbSub010200_03.Checked.ToString());
                m_Sub010200ALInit.Add(ckbSub010200_04.Checked.ToString());
                m_Sub010200ALInit.Add(ckbSub010200_05.Checked.ToString());
                m_Sub010200ALInit.Add(ckbSub010200_06.Checked.ToString());
                m_Sub010200ALInit.Add(ckbSub010200_07.Checked.ToString());
                m_Sub010200ALInit.Add(ckbSub010200_08.Checked.ToString());
                m_Sub010200ALInit.Add(ckbSub010200_09.Checked.ToString());
                m_Sub010200ALInit.Add(ckbSub010200_10.Checked.ToString());
                m_Sub010200ALInit.Add(ckbSub010200_11.Checked.ToString());
                m_Sub010200ALInit.Add(rdbSub010200_01.Checked.ToString());
                m_Sub010200ALInit.Add(rdbSub010200_02.Checked.ToString());
                m_Sub010200ALInit.Add(rdbSub010200_03.Checked.ToString());
                m_Sub010200ALInit.Add(rdbSub010200_04.Checked.ToString());
                m_Sub010200ALInit.Add(steSub010200_01.StrValue1 + steSub010200_01.StrValue2);
                m_Sub010200ALInit.Add(steSub010200_02.StrValue1 + steSub010200_02.StrValue2);
                m_Sub010200ALInit.Add(steSub010200_03.StrValue1 + steSub010200_03.StrValue2);
                //--
            }
        }
        private void butSub0102_01_Click(object sender, EventArgs e)//編修卡片
        {
            if (m_intcard_id <= 0)
            {
                try
                {
                    int index = dgvSub0102_01.SelectedRows[0].Index;//取得被選取的第一列位置
                    String Strcard_id = dgvSub0102_01.Rows[index].Cells[1].Value.ToString();
                    m_intcard_id = Int32.Parse(Strcard_id);
                }
                catch
                {
                }
            }
            modifiedCardData();
        }

        private void butSub0102_02_Click(object sender, EventArgs e)//新增卡片
        {
            m_TPOld = m_tabMain.SelectedTab;//--2017/02/22 製作返回按鈕功能
            TabPage_Push();//m_StackTPOld.Push(m_TPOld);//--2017/02/22 製作返回按鈕功能

            m_tabSub010200.Parent = m_tabMain;
            labSub010200_08.ReadOnly = false;//可編輯卡片內碼
            m_intcard_id = -10;
            initSub010200UI();
            m_tabMain.SelectedTab = m_tabSub010200;

            //--
            //add at 2017/10/12
            m_Sub010200ALInit.Clear();
            m_Sub010200ALInit.Add(labSub010200_07.Text);
            m_Sub010200ALInit.Add(labSub010200_08.Text);
            m_Sub010200ALInit.Add(txtSub010200_01.Text);
            m_Sub010200ALInit.Add(txtSub010200_02.Text);
            m_Sub010200ALInit.Add(txtSub010200_03.Text);
            m_Sub010200ALInit.Add(cmbSub010200_01.SelectedIndex + "");
            m_Sub010200ALInit.Add(adpSub010200_01.Value.ToString("yyyy-MM-dd HH:mm"));
            m_Sub010200ALInit.Add(adpSub010200_02.Value.ToString("yyyy-MM-dd HH:mm"));
            m_Sub010200ALInit.Add(ckbSub010200_01.Checked.ToString());
            m_Sub010200ALInit.Add(ckbSub010200_02.Checked.ToString());
            m_Sub010200ALInit.Add(ckbSub010200_03.Checked.ToString());
            m_Sub010200ALInit.Add(ckbSub010200_04.Checked.ToString());
            m_Sub010200ALInit.Add(ckbSub010200_05.Checked.ToString());
            m_Sub010200ALInit.Add(ckbSub010200_06.Checked.ToString());
            m_Sub010200ALInit.Add(ckbSub010200_07.Checked.ToString());
            m_Sub010200ALInit.Add(ckbSub010200_08.Checked.ToString());
            m_Sub010200ALInit.Add(ckbSub010200_09.Checked.ToString());
            m_Sub010200ALInit.Add(ckbSub010200_10.Checked.ToString());
            m_Sub010200ALInit.Add(ckbSub010200_11.Checked.ToString());
            m_Sub010200ALInit.Add(rdbSub010200_01.Checked.ToString());
            m_Sub010200ALInit.Add(rdbSub010200_02.Checked.ToString());
            m_Sub010200ALInit.Add(rdbSub010200_03.Checked.ToString());
            m_Sub010200ALInit.Add(rdbSub010200_04.Checked.ToString());
            m_Sub010200ALInit.Add(steSub010200_01.StrValue1 + steSub010200_01.StrValue2);
            m_Sub010200ALInit.Add(steSub010200_02.StrValue1 + steSub010200_02.StrValue2);
            m_Sub010200ALInit.Add(steSub010200_03.StrValue1 + steSub010200_03.StrValue2);
            //--
        }

        private void butSub0102_03_Click_XX(object sender, EventArgs e)//匯入卡片[csv]-按鈕內
        {
            String StrPath;
            //String StrUID, StrName, StrType, StrStatus;
            String SQL = "";
            OpenFileDialog openFileDialog1 = new OpenFileDialog();
            openFileDialog1.Filter = "CSV File|*.csv";
            openFileDialog1.Title = "Open an CSV";
            openFileDialog1.RestoreDirectory = true;
            int intindex = 0;
            if (openFileDialog1.ShowDialog() == DialogResult.OK)
            {
                StrPath = openFileDialog1.FileName.ToString();
                // 建立檔案串流（@ 可取消跳脫字元 escape sequence）
                StreamReader sr = new StreamReader(StrPath);
                String StrTitle = "";
                while (!sr.EndOfStream)// 每次讀取一行，直到檔尾
                {
                    String line = sr.ReadLine();// 讀取文字到 line 變數
                    
                    //--
                    //卡片資料表全部(id和state除外)匯入
                    String Data = "";
                    if (intindex == 0)
                    {
                        StrTitle = line;
                    }
                    else
                    {
                        if ((StrTitle.Length>0) && (line.Length > 0))
                        {
                            //---
                            //修正『相同卡號可重複匯入』BUG
                            string[] strs = line.Split(',');
                            String checkSQL = String.Format("SELECT card_code FROM card WHERE card_code='{0}';", strs[0]);
                            MySqlDataReader Reader_Data = MySQL.GetDataReader(checkSQL);
                            if (!Reader_Data.HasRows)
                            {
                                Data = "'" + line + "'";
                                Data = Data.Replace(",", "','");
                                SQL += String.Format("INSERT INTO card ({0}) VALUES ({1});", StrTitle, Data);
                            }
                            Reader_Data.Close();
                            //Data = "'" + line + "'";
                            //Data = Data.Replace(",", "','");
                            //SQL += String.Format("INSERT INTO card ({0}) VALUES ({1});", StrTitle, Data);
                            //---修正『相同卡號可重複匯入』BUG

                        }
                    }
                    //--

                    /*
                    //--
                    //2017/12/26之前的卡片匯入
                    string[] strs = line.Split(',');
                    if ((strs.Length > 3) && (intindex > 0))
                    {
                        StrUID = strs[0];
                        if (StrUID.Length < 16)
                        {
                            StrUID = StrUID.PadLeft(16, '0');
                        }
                        StrName = strs[1];
                        StrType = strs[2];
                        StrStatus = strs[3];
                        if (StrStatus == "")
                        {
                            StrStatus = "1";
                        }
                        
                        SQL += String.Format("INSERT INTO card (card_code, display, type, active, available_date_start, available_date_end, state) VALUES ('{0}','{1}','{2}','{3}','{4}','{4}',1);", StrUID, StrName, StrType, StrStatus, DateTime.Now.ToString("yyyy-MM-dd HH:mm"));
                    }
                    //--
                    */
 
                    intindex++;

                    if ((intindex == 50) && (SQL.Length > 0))
                    {
                        MySQL.InsertUpdateDelete(SQL);//新增資料
                        SQL = "";
                        intindex = 1;
                    }
                }

                if (SQL.Length > 0)//最後未滿49筆的新增
                {
                    MySQL.InsertUpdateDelete(SQL);//新增資料
                    SQL = "";
                }

                sr.Close();// 關閉串流
                get_show_Cards();//取得卡片列表
            }
        }

        //---
        //修改卡片匯入要變成執行序模式+即時進度顯示
        public String m_StrImportCardCSVPath = "";
        public void ImportCardCSV()
        {
            String SQL = "";
            int intindex = 0;
            StreamReader sr = new StreamReader(m_StrImportCardCSVPath);
            String StrTitle = "";
            while (!sr.EndOfStream)// 每次讀取一行，直到檔尾
            {
                String line = sr.ReadLine();// 讀取文字到 line 變數

                //--
                //卡片資料表全部(id和state除外)匯入
                String Data = "";
                if (intindex == 0)
                {
                    StrTitle = line;
                }
                else
                {
                    if ((StrTitle.Length > 0) && (line.Length > 0))
                    {
                        //---
                        //修正『相同卡號可重複匯入』BUG
                        string[] strs = line.Split(',');
                        String checkSQL = String.Format("SELECT card_code FROM card WHERE card_code='{0}';", strs[0]);
                        MySqlDataReader Reader_Data = MySQL.GetDataReader(checkSQL);
                        if (!Reader_Data.HasRows)
                        {
                            Data = "'" + line + "'";
                            Data = Data.Replace(",", "','");
                            SQL += String.Format("INSERT INTO card ({0}) VALUES ({1});", StrTitle, Data);
                        }
                        Reader_Data.Close();
                        //Data = "'" + line + "'";
                        //Data = Data.Replace(",", "','");
                        //SQL += String.Format("INSERT INTO card ({0}) VALUES ({1});", StrTitle, Data);
                        //---修正『相同卡號可重複匯入』BUG

                    }
                }
                //--

                /*
                //--
                //2017/12/26之前的卡片匯入
                string[] strs = line.Split(',');
                if ((strs.Length > 3) && (intindex > 0))
                {
                    StrUID = strs[0];
                    if (StrUID.Length < 16)
                    {
                        StrUID = StrUID.PadLeft(16, '0');
                    }
                    StrName = strs[1];
                    StrType = strs[2];
                    StrStatus = strs[3];
                    if (StrStatus == "")
                    {
                        StrStatus = "1";
                    }
                        
                    SQL += String.Format("INSERT INTO card (card_code, display, type, active, available_date_start, available_date_end, state) VALUES ('{0}','{1}','{2}','{3}','{4}','{4}',1);", StrUID, StrName, StrType, StrStatus, DateTime.Now.ToString("yyyy-MM-dd HH:mm"));
                }
                //--
                */

                intindex++;

                if ((intindex == 50) && (SQL.Length > 0))
                {
                    MySQL.InsertUpdateDelete(SQL);//新增資料
                    SQL = "";
                    intindex = 1;
                }
            }

            if (SQL.Length > 0)//最後未滿49筆的新增
            {
                MySQL.InsertUpdateDelete(SQL);//新增資料
                SQL = "";
            }

            sr.Close();// 關閉串流
            
        }
        private void butSub0102_03_Click(object sender, EventArgs e)//匯入卡片[csv]
        {
            OpenFileDialog openFileDialog1 = new OpenFileDialog();
            openFileDialog1.Filter = "CSV File|*.csv";
            openFileDialog1.Title = "Open an CSV";
            openFileDialog1.RestoreDirectory = true;
            if (openFileDialog1.ShowDialog() == DialogResult.OK)
            {
                m_StrImportCardCSVPath = openFileDialog1.FileName.ToString();
                Animation.createThreadAnimation(butSub0102_03.Text, Animation.Thread_ImportCardCSV);
                get_show_Cards();//取得卡片列表
            }
        }
        //---修改卡片匯入要變成執行序模式+即時進度顯示

        private void butSub0102_04_Click(object sender, EventArgs e)//匯出卡片
        {
            String StrPath = "";
            String StrUID, StrName, StrType, StrStatus;
            String SQL = "";
            SaveFileDialog saveFileDialog1 = new SaveFileDialog();
            saveFileDialog1.Filter = "CSV File|*.csv";
            saveFileDialog1.Title = "Save an CSV";
            saveFileDialog1.FileName = "card.csv";
            if (saveFileDialog1.ShowDialog() == DialogResult.OK)
            {
                StrPath = saveFileDialog1.FileName.ToString();
                StreamWriter sw = new StreamWriter(StrPath, false, System.Text.Encoding.UTF8);
                
                //--
                //卡片資料表全部(id和state除外)匯出
                SQL = "SELECT * FROM card ORDER BY card_code ASC;";
                String StrTitle="";
                MySqlDataReader Reader_Data = MySQL.GetDataReader(SQL);
                for(int i=0;i<(Reader_Data.VisibleFieldCount-1);i++)
                {
                    if(i>1)
                    {
                        StrTitle += ",";
                    }
                    if (i > 0)
                    {
                        StrTitle += Reader_Data.GetName(i);
                    }
                }
                sw.WriteLine(StrTitle);

                while (Reader_Data.Read())
                {
                    String Data="";
                    for(int j=0;j<(Reader_Data.VisibleFieldCount-1);j++)
                    {
                        if(j>1)
                        {
                            Data += ",";
                        }
                        if (j > 0)
                        {
                            Data += Reader_Data[j].ToString();
                        }
                    }
                    if (Data.Length > 0)
                    {
                        sw.WriteLine(Data);
                    }
                }
                Reader_Data.Close();
                //--

                /*
                //--
                //2017/12/26之前版本的卡片匯出功能
                sw.WriteLine("卡片內碼(16位),卡片名稱,卡片類型,狀態");
                SQL = "SELECT card_code, display, type, active FROM card ORDER BY card_code ASC;";
                MySqlDataReader Reader_Data = MySQL.GetDataReader(SQL);
                while (Reader_Data.Read())
                {
                    StrUID = Reader_Data["card_code"].ToString();
                    StrName = Reader_Data["display"].ToString();
                    StrType = Reader_Data["type"].ToString();
                    StrStatus = Reader_Data["active"].ToString();
                    String Data = StrUID + "," + StrName + "," + StrType + "," + StrStatus;
                    sw.WriteLine(Data);
                }
                Reader_Data.Close();
                //--
                */

                sw.Close();
            }
        }

        private void butSub0102_06_Click(object sender, EventArgs e)//全選卡片列表
        {
            /*
            for (int i = 0; i < dgvSub0102_01.Rows.Count; i++)
            {
                dgvSub0102_01.Rows[i].Cells[0].Value = true;
                dgvSub0102_01.Rows[i].Selected = true;
            }
            */
            dgvSub0102_01.SelectAll();//把所有列表元件的全選/取消全選改成SelectAll()/ClearSelection()
        }

        private void butSub0102_07_Click(object sender, EventArgs e)//取消全選卡片列表
        {
            /*
            for (int i = 0; i < dgvSub0102_01.Rows.Count; i++)
            {
                dgvSub0102_01.Rows[i].Cells[0].Value = false;
                dgvSub0102_01.Rows[i].Selected = false;
            }
            */
            dgvSub0102_01.ClearSelection();//把所有列表元件的全選/取消全選改成SelectAll()/ClearSelection()
        }

        private void butSub0102_08_Click(object sender, EventArgs e)//卡片列表-批次執行
        {
            String SQL = "";
            int count = 0;
            ArrayList ALBuf = new ArrayList();
            ALBuf.Clear();
            for (int i = 0; i < dgvSub0102_01.Rows.Count; i++)
            {
                String data = dgvSub0102_01.Rows[i].Cells[0].Value.ToString().ToLower();
                if (data == "true")
                {
                    ALBuf.Add(dgvSub0102_01.Rows[i].Cells[1].Value.ToString());
                }
            }
            switch (cmbSub0102_01.SelectedIndex)
            {
                case 0://Delete Selected
                    for (int i = 0; i < ALBuf.Count; i++)
                    {
                        count++;
                        SQL += String.Format("DELETE FROM card WHERE id={0};", ALBuf[i].ToString());
                        if ((count == 50) && (SQL.Length > 0))
                        {
                            MySQL.InsertUpdateDelete(SQL);
                            SQL = "";
                            count = 0;
                        }
                    }
                    if (SQL.Length > 0)
                    {
                        MySQL.InsertUpdateDelete(SQL);
                    }
                    break;
                case 1://Enable Selected
                    for (int i = 0; i < ALBuf.Count; i++)
                    {
                        count++;
                        SQL += String.Format("UPDATE card SET active=1 WHERE id={0};", ALBuf[i].ToString());
                        if ((count == 50) && (SQL.Length > 0))
                        {
                            MySQL.InsertUpdateDelete(SQL);
                            SQL = "";
                            count = 0;
                        }
                    }
                    if (SQL.Length > 0)
                    {
                        MySQL.InsertUpdateDelete(SQL);
                    }
                    break;
                case 2://Disable Selected
                    for (int i = 0; i < ALBuf.Count; i++)
                    {
                        count++;
                        SQL += String.Format("UPDATE card SET active=0 WHERE id={0};", ALBuf[i].ToString());
                        if ((count == 50) && (SQL.Length > 0))
                        {
                            MySQL.InsertUpdateDelete(SQL);
                            SQL = "";
                            count = 0;
                        }
                    }
                    if (SQL.Length > 0)
                    {
                        MySQL.InsertUpdateDelete(SQL);
                    }
                    break;
            }
            get_show_Cards();//取得卡片列表
        }

        private void dgvSub0102_01_SelectionChanged(object sender, EventArgs e)//卡片列表選擇改變事件
        {
            //---
            //列表元件shift全選時最前面的狀態要能自動打勾，反之要能取消
            for (int i = 0; i < dgvSub0102_01.Rows.Count; i++)
            {
                dgvSub0102_01.Rows[i].Cells[0].Value = false;
            }
            for (int j = 0; j < dgvSub0102_01.SelectedRows.Count; j++)
            {
                dgvSub0102_01.SelectedRows[j].Cells[0].Value = true;
            }
            //---列表元件shift全選時最前面的狀態要能自動打勾，反之要能取消

            try
            {
                int index = dgvSub0102_01.SelectedRows[0].Index;//取得被選取的第一列位置
                String Strcard_id = dgvSub0102_01.Rows[index].Cells[1].Value.ToString();
                m_intcard_id = Int32.Parse(Strcard_id);
            }
            catch
            {
            }
        }
        
        public String m_SQL_card_condition01 = "";
        private void ckbSub0102_01_CheckedChanged(object sender, EventArgs e)//所有卡片列表 SQL延伸過濾選項事件
        {
            butSub0102_09.PerformClick();
        }

        private void butSub0102_09_Click_XX(object sender, EventArgs e)//所有卡片列表 畫面搜尋
        {
            get_show_Cards();
            ArrayList AL01 = new ArrayList();
            ArrayList AL02 = new ArrayList();
            ArrayList AL03 = new ArrayList();
            ArrayList AL04 = new ArrayList();
            ArrayList AL05 = new ArrayList();
            ArrayList AL06 = new ArrayList();
            ArrayList AL07 = new ArrayList();
            if (txtSub0102_01.Text != "")
            {
                for (int i = 0; i < dgvSub0102_01.Rows.Count; i++)//取的現行UI上卡片列表所有資料
                {
                    AL01.Add(dgvSub0102_01.Rows[i].Cells[1].Value.ToString());
                    AL02.Add(dgvSub0102_01.Rows[i].Cells[2].Value.ToString());
                    AL03.Add(dgvSub0102_01.Rows[i].Cells[3].Value.ToString());
                    AL04.Add(dgvSub0102_01.Rows[i].Cells[4].Value.ToString());
                    AL05.Add(dgvSub0102_01.Rows[i].Cells[5].Value.ToString());
                    AL06.Add(dgvSub0102_01.Rows[i].Cells[6].Value.ToString());
                    AL07.Add(dgvSub0102_01.Rows[i].Cells[7].Value.ToString());
                }
                cleandgvSub0102_01();//清空畫面
                String StrSearch = txtSub0102_01.Text;
                for (int i = 0; i < AL01.Count; i++)
                {
                    if ((AL02[i].ToString().IndexOf(StrSearch) > -1) || (AL03[i].ToString().IndexOf(StrSearch) > -1) || (AL04[i].ToString().IndexOf(StrSearch) > -1) || (AL05[i].ToString().IndexOf(StrSearch) > -1) || (AL06[i].ToString().IndexOf(StrSearch) > -1) || (AL07[i].ToString().IndexOf(StrSearch) > -1))
                    {
                        dgvSub0102_01.Rows.Add(false, AL01[i].ToString(), AL02[i].ToString(), AL03[i].ToString(), AL04[i].ToString(), AL05[i].ToString(), AL06[i].ToString(), AL07[i].ToString());
                    }
                }
            }
        }

        private void butSub0102_09_Click(object sender, EventArgs e)//所有卡片列表 DB搜尋
        {
            m_intCardNowPage = 1;
            m_SQL_card_condition01 = "";
            if (ckbSub0102_01.Checked == true || ckbSub0102_02.Checked == true || ckbSub0102_03.Checked == true || ckbSub0102_04.Checked == true)
            {
                m_SQL_card_condition01 = "WHERE";
                if (ckbSub0102_01.Checked == true)
                {
                    if (ckbSub0102_02.Checked == true)
                    {//兩個都選等於沒作用
                        m_SQL_card_condition01 += " c.active>=0";
                    }
                    else
                    {
                        m_SQL_card_condition01 += " c.active=1";
                    }
                }
                else
                {
                    if (ckbSub0102_02.Checked == true)
                    {
                        m_SQL_card_condition01 += " c.active=0";
                    }
                    else
                    {//兩個都沒選等於沒作用
                        m_SQL_card_condition01 += " c.active>=0";
                    }
                }


                /*解決卡片列表過濾條件失效問題
                if (m_SQL_card_condition01.IndexOf("active") > 0)
                {
                    m_SQL_card_condition01 += " AND";
                }
                */

                if (ckbSub0102_03.Checked == true)
                {
                    if (ckbSub0102_04.Checked == true)
                    {//兩個都選等於沒作用
                        m_SQL_card_condition01 += "";//解決卡片列表過濾條件失效問題-m_SQL_card_condition01 += " c.type>=0";
                    }
                    else
                    {
                        m_SQL_card_condition01 += " AND (c.id IN (SELECT card_id AS id FROM card_for_user_car))";//解決卡片列表過濾條件失效問題-m_SQL_card_condition01 += " c.type>0";
                    }
                }
                else
                {
                    if (ckbSub0102_04.Checked == true)
                    {
                        m_SQL_card_condition01 += " AND (c.id NOT IN (SELECT card_id AS id FROM card_for_user_car))";//解決卡片列表過濾條件失效問題-m_SQL_card_condition01 += " c.type=0";
                    }
                    else
                    {//兩個都沒選等於沒作用
                        m_SQL_card_condition01 += "";//解決卡片列表過濾條件失效問題-m_SQL_card_condition01 += " c.type>=0";
                    }
                }
            }
            if (txtSub0102_01.Text != "")
            {
                if (m_SQL_card_condition01.Length > 0)
                {
                    m_SQL_card_condition01 += String.Format(" AND ((c.card_code LIKE '%{0}%') OR (c.display LIKE '%{0}%') OR (c.alias LIKE '%{0}%') OR (c_t.type_name LIKE '%{0}%') OR (c.available_date_start LIKE '%{0}%') OR (c.available_date_end LIKE '%{0}%'))", txtSub0102_01.Text);
                }
                else
                {
                    m_SQL_card_condition01 = String.Format("WHERE ((c.card_code LIKE '%{0}%') OR (c.display LIKE '%{0}%') OR (c.alias LIKE '%{0}%') OR (c_t.type_name LIKE '%{0}%') OR (c.available_date_start LIKE '%{0}%') OR (c.available_date_end LIKE '%{0}%'))", txtSub0102_01.Text);
                }
            }
            get_show_Cards();
        }
        private void butSub0102_11_Click(object sender, EventArgs e)//卡片列表移至第一頁
        {
            m_intCardNowPage = 1;
            get_show_Cards();
        }

        private void butSub0102_12_Click(object sender, EventArgs e)//卡片列表移至前一頁
        {
            m_intCardNowPage--;
            if (m_intCardNowPage < 1)
            {
                m_intCardNowPage = 1;
            }
            get_show_Cards();
        }

        private void butSub0102_13_Click(object sender, EventArgs e)//卡片列表移至後一頁
        {
            m_intCardNowPage++;
            if (m_intCardNowPage > m_intCardAllPage)
            {
                m_intCardNowPage = m_intCardAllPage;
            }
            get_show_Cards();
        }

        private void butSub0102_14_Click(object sender, EventArgs e)//卡片列表移至最後頁
        {
            m_intCardNowPage = m_intCardAllPage;
            get_show_Cards();
        }
        //Sub0102_end
        //Sub010200_start
        public bool CardData2DB(bool blnRunSQL=true,int state=1)
        {
            bool blnAns = false;
            String Strname, Strcard_code, Stralias, Strpin, Strdisplay, Strtype, Stravailable_date_start, Stravailable_date_end, Stractive, Strblock, Strapb_enable, Strlevel, Strweek_plan, Straccess_time_1_start, Straccess_time_1_end, Straccess_time_2_start, Straccess_time_2_end, Straccess_time_3_start, Straccess_time_3_end;
            
            Strname = labSub010200_07.Text;

            Strcard_code = labSub010200_08.Text.PadLeft(16, '0').ToUpper();//補齊16個字且全部轉大寫

            Stralias = txtSub010200_01.Text;

            Strpin = txtSub010200_02.StrValue.PadRight(16, 'F');//補齊16個字
            
            Strdisplay = txtSub010200_03.Text;
            
            if(cmbSub010200_01.SelectedIndex>=0)
            {
                Strtype = m_ALCardType_ID[cmbSub010200_01.SelectedIndex].ToString();
            }
            else
            {
                Strtype = "0";
            }

            Stravailable_date_start = adpSub010200_01.Value.ToString("yyyy-MM-dd HH:mm");//效期
            Stravailable_date_end = adpSub010200_02.Value.ToString("yyyy-MM-dd HH:mm");

            Stractive = "0";
            if (ckbSub010200_01.Checked == false)
            {
                Stractive = "1";
            }

            Strblock = "0";
            if (ckbSub010200_02.Checked==true)
            {
                Strblock = "1";
            }

            Strapb_enable = "0";
            if (ckbSub010200_03.Checked == false)
            {
                Strapb_enable = "1";
            }

            Strlevel = "0";
            if (rdbSub010200_01.Checked == true)
            {
                Strlevel = "0";
            }
            if (rdbSub010200_02.Checked == true)
            {
                Strlevel = "1";
            }
            if (rdbSub010200_03.Checked == true)
            {
                Strlevel = "2";
            }
            if (rdbSub010200_04.Checked == true)
            {
                Strlevel = "3";
            }

            int v1 = 0;
            v1 += Convert.ToInt32(ckbSub010200_04.Checked) * 1;
            v1 += Convert.ToInt32(ckbSub010200_05.Checked) * 2;
            v1 += Convert.ToInt32(ckbSub010200_06.Checked) * 4;
            v1 += Convert.ToInt32(ckbSub010200_07.Checked) * 8;
            v1 += Convert.ToInt32(ckbSub010200_08.Checked) * 16;
            v1 += Convert.ToInt32(ckbSub010200_09.Checked) * 32;
            v1 += Convert.ToInt32(ckbSub010200_10.Checked) * 64;
            v1 += Convert.ToInt32(ckbSub010200_11.Checked) * 128;
            Strweek_plan = Convert.ToString(v1, 2).PadLeft(8, '0');

            Straccess_time_1_start = steSub010200_01.StrValue1 + ":00";
            Straccess_time_1_end = steSub010200_01.StrValue2 + ":00";

            Straccess_time_2_start = steSub010200_02.StrValue1 + ":00";
            Straccess_time_2_end = steSub010200_02.StrValue2 + ":00";

            Straccess_time_3_start = steSub010200_03.StrValue1 + ":00";
            Straccess_time_3_end = steSub010200_03.StrValue2 + ":00";

            //--
            //add at 2017/10/12
            if (!blnRunSQL)
            {
                m_Sub010200ALData.Clear();
                m_Sub010200ALData.Add(labSub010200_07.Text);
                m_Sub010200ALData.Add(labSub010200_08.Text);
                m_Sub010200ALData.Add(txtSub010200_01.Text);
                m_Sub010200ALData.Add(txtSub010200_02.Text);
                m_Sub010200ALData.Add(txtSub010200_03.Text);
                m_Sub010200ALData.Add(cmbSub010200_01.SelectedIndex + "");
                m_Sub010200ALData.Add(adpSub010200_01.Value.ToString("yyyy-MM-dd HH:mm"));
                m_Sub010200ALData.Add(adpSub010200_02.Value.ToString("yyyy-MM-dd HH:mm"));
                m_Sub010200ALData.Add(ckbSub010200_01.Checked.ToString());
                m_Sub010200ALData.Add(ckbSub010200_02.Checked.ToString());
                m_Sub010200ALData.Add(ckbSub010200_03.Checked.ToString());
                m_Sub010200ALData.Add(ckbSub010200_04.Checked.ToString());
                m_Sub010200ALData.Add(ckbSub010200_05.Checked.ToString());
                m_Sub010200ALData.Add(ckbSub010200_06.Checked.ToString());
                m_Sub010200ALData.Add(ckbSub010200_07.Checked.ToString());
                m_Sub010200ALData.Add(ckbSub010200_08.Checked.ToString());
                m_Sub010200ALData.Add(ckbSub010200_09.Checked.ToString());
                m_Sub010200ALData.Add(ckbSub010200_10.Checked.ToString());
                m_Sub010200ALData.Add(ckbSub010200_11.Checked.ToString());
                m_Sub010200ALData.Add(rdbSub010200_01.Checked.ToString());
                m_Sub010200ALData.Add(rdbSub010200_02.Checked.ToString());
                m_Sub010200ALData.Add(rdbSub010200_03.Checked.ToString());
                m_Sub010200ALData.Add(rdbSub010200_04.Checked.ToString());
                m_Sub010200ALData.Add(steSub010200_01.StrValue1 + steSub010200_01.StrValue2);
                m_Sub010200ALData.Add(steSub010200_02.StrValue1 + steSub010200_02.StrValue2);
                m_Sub010200ALData.Add(steSub010200_03.StrValue1 + steSub010200_03.StrValue2);
                return (!blnRunSQL);
            }
            //--

            String SQL = "";
            if (m_intcard_id<0)//新增
            {
                SQL = String.Format("SELECT COUNT(id) AS number FROM card WHERE card_code = '{0}';", Strcard_code);
                MySqlDataReader DataReader = MySQL.GetDataReader(SQL);
                SQL = String.Format("INSERT INTO card (card_code, alias, pin, display, type, available_date_start, available_date_end, active, block, apb_enable, level, week_plan, access_time_1_start, access_time_1_end, access_time_2_start, access_time_2_end, access_time_3_start, access_time_3_end,state) VALUES ('{0}','{1}','{2}','{3}','{4}','{5}','{6}','{7}','{8}','{9}','{10}','{11}','{12}','{13}','{14}','{15}','{16}','{17}',{18});", Strcard_code, Stralias, Strpin, Strdisplay, Strtype, Stravailable_date_start, Stravailable_date_end, Stractive, Strblock, Strapb_enable, Strlevel, Strweek_plan, Straccess_time_1_start, Straccess_time_1_end, Straccess_time_2_start, Straccess_time_2_end, Straccess_time_3_start, Straccess_time_3_end, state);
                //SQL = String.Format("INSERT INTO card (card_code, alias, pin, display, type, available_date_start, available_date_end, active, block, apb_enable, level, week_plan, access_time_1_start, access_time_1_end, access_time_2_start, access_time_2_end, access_time_3_start, access_time_3_end,state) SELECT '{0}','{1}','{2}','{3}','{4}','{5}','{6}','{7}','{8}','{9}','{10}','{11}','{12}','{13}','{14}','{15}','{16}','{17}',{18} FROM DUAL WHERE NOT EXISTS(SELECT id FROM card WHERE card_code = '{0}');", Strcard_code, Stralias, Strpin, Strdisplay, Strtype, Stravailable_date_start, Stravailable_date_end, Stractive, Strblock, Strapb_enable, Strlevel, Strweek_plan, Straccess_time_1_start, Straccess_time_1_end, Straccess_time_2_start, Straccess_time_2_end, Straccess_time_3_start, Straccess_time_3_end, state);               
                while (DataReader.Read())
                {
                    if (DataReader["number"].ToString() != "0")
                    {
                        SQL = "";
                        //---
                        //修正卡片內碼重複會沒反應的BUG
                        labSub010200_08.Text = "";
                        labSub010200_02.ForeColor = Color.Red;
                        //---修正卡片內碼重複會沒反應的BUG
                        MessageBox.Show(Language.m_StrCardData2DBMsg00, butSub010200_05.Text, MessageBoxButtons.OK, MessageBoxIcon.Error);//修正『輸入重複的卡片內碼，系統僅會顯示紅色字體，應要顯示"重複卡片"提示』BUG
                    }
                    break;
                }
                DataReader.Close();
            }
            else//修改
            {
                SQL = String.Format("UPDATE card SET card_code='{0}', alias='{1}', pin='{2}', display='{3}', type='{4}', available_date_start='{5}', available_date_end='{6}', active='{7}', block='{8}', apb_enable='{9}', level='{10}', week_plan='{11}', access_time_1_start='{12}', access_time_1_end='{13}', access_time_2_start='{14}', access_time_2_end='{15}', access_time_3_start='{16}', access_time_3_end='{17}', state={18} WHERE id={19};", Strcard_code, Stralias, Strpin, Strdisplay, Strtype, Stravailable_date_start, Stravailable_date_end, Stractive, Strblock, Strapb_enable, Strlevel, Strweek_plan, Straccess_time_1_start, Straccess_time_1_end, Straccess_time_2_start, Straccess_time_2_end, Straccess_time_3_start, Straccess_time_3_end, state, m_intcard_id);
            }

            if(SQL.Length>0)
            {
                blnAns = MySQL.InsertUpdateDelete(SQL);

                //--
                //add 2017/11/01
                if ((m_intUserAddCard_id == -10) || (m_intUserAddCard_id > 0))
                {
                    String StrCard_id = "";
                    SQL = String.Format("SELECT id FROM card WHERE card_code = '{0}';", Strcard_code);
                    MySqlDataReader Reader_id = MySQL.GetDataReader(SQL);
                    while (Reader_id.Read())
                    {
                        StrCard_id = Reader_id["id"].ToString();
                        break;
                    }
                    Reader_id.Close();
                    SQL = String.Format("INSERT INTO card_for_user_car (card_id,user_id,status,state) VALUES ({0},{1},1,1);", StrCard_id, m_intUserAddCard_id);
                    MySQL.InsertUpdateDelete(SQL);
                }
                //--

            }
            return blnAns;
        }

        private void butSub010200_05_Click(object sender, EventArgs e)//卡片編輯儲存
        {
            if(CardData2DB())
            {
                m_intcard_id = -1;
                get_show_Cards();//取得卡片列表
                get_show_UserCards(m_intuser_id);//取得人員的卡片列表
                get_show_CarCards(m_intcar_id);//取得車輛的卡片列表
                if ((m_ALUserFP != null) && m_ALUserFP.Count > 0)
                {
                    UserFP2cmbSub0400_01Select(m_intUserAddCard_id);//指紋UI可以新增卡片功能
                }
                Leave_function();
            }
        }

        //--
        //卡片內碼元件要能支援C/P
        private void labSub010200_08_KeyUp(object sender, KeyEventArgs e)
        {
            //https://dotblogs.com.tw/chou/2011/12/20/62709
            //http://www.cnblogs.com/han1982/p/4770270.html
            //https://fredxxx123.wordpress.com/2008/11/22/c-%E8%A4%87%E8%A3%BD%E8%B3%87%E6%96%99%E5%88%B0%E5%89%AA%E8%B2%BC%E7%B0%BF/
            //https://social.msdn.microsoft.com/Forums/zh-TW/3cc1d2be-5be7-4388-831e-2b5485b3b509/-textbox-?forum=233
            if (e.KeyData == (Keys.Control | Keys.A))
            {
                labSub010200_08.SelectAll();
            }
            if (e.KeyData == (Keys.Control | Keys.C))
            {
                //MessageBox.Show("Ctrl + C");
                Clipboard.SetData(DataFormats.Text, labSub010200_08.Text);
            }
            if (e.KeyData == (Keys.Control | Keys.V))//偵測Ctrl+v
            {
                //MessageBox.Show("Ctrl + V");
                if (Clipboard.ContainsText())
                {
                    try
                    {
                        Convert.ToInt32(Clipboard.GetText());  //检查是否数字
                        ((TextBox)sender).SelectedText = Clipboard.GetText().Trim(); //Ctrl+V 粘贴  
                        if (((TextBox)sender).TextLength > 16)
                        {
                            ((TextBox)sender).Text = ((TextBox)sender).Text.Remove(16); //TextBox最大长度为16  移除多余的
                        }
                    }
                    catch (Exception)
                    {
                        e.Handled = true;
                        //throw;
                    }
                }
            }
        }
        //--

        private void labSub010200_08_KeyPress(object sender, KeyPressEventArgs e)//卡片內碼限制
        {
            if (e.KeyChar == 8)//刪除鍵要直接允許
            {
                e.Handled = false;
            }
            else
            {
                if (labSub010200_08.Text.Length < 16)//長度限制在16
                {
                    if ((e.KeyChar >= 'a' && e.KeyChar <= 'f') || (e.KeyChar >= 'A' && e.KeyChar <= 'F') || (e.KeyChar >= '0' && e.KeyChar <= '9'))//限制0~9和A~F
                    {
                        e.Handled = false;
                    }
                    else
                    {
                        e.Handled = true;
                    }
                }
                else
                {
                    e.Handled = true;
                }
            }
        }

        private void butSub010200_07_Click(object sender, EventArgs e)//新增/編修卡片 離開按鈕
        {
            CardData2DB(false);//add at 2017/10/12

            if ( (m_intcard_id == -1) || CheckUIVarNotChange(m_Sub010200ALInit, m_Sub010200ALData) )//if (m_intcard_id == -1)
            {
                m_intUserAddCard_id = 0;//add 2017/11/01
                Leave_function();
            }
            else
            {
                DialogResult myResult = MessageBox.Show(Language.m_StrControllerMsg00, butSub010200_07.Text, MessageBoxButtons.YesNo, MessageBoxIcon.Question);
                if (myResult == DialogResult.Yes)
                {
                    m_intUserAddCard_id = 0;//add 2017/11/01
                    Leave_function();
                }
            }
        }
        //Sub010200_end
        //Sub0101_start
        private void dgvSub0101_01_DoubleClick(object sender, EventArgs e)//at 2017/09/15
        {
            butSub0101_01.PerformClick();
        }
        public int m_intcar_id = -1;//紀錄car_id
        private void butSub0101_01_Click(object sender, EventArgs e)//車輛列表UI-編輯
        {
            FileLib.DeleteFile("temp.png");//徹底刪除人員車輛照片暫存檔-2018/04/02防呆用

            String SQL;
            m_intcar_id = -1;
            m_intdep_id = -1;
            m_intuser_id = -1;
            if (m_intcar_id < 0)
            {
                try
                {
                    int index = dgvSub0101_01.SelectedRows[0].Index;//取得被選取的第一列位置
                    String Strcar_id = dgvSub0101_01.Rows[index].Cells[1].Value.ToString();
                    m_intcar_id = Int32.Parse(Strcar_id);
                    SQL = String.Format("SELECT user_id,dep_id FROM department_detail WHERE car_id='{0}';", m_intcar_id);
                    MySqlDataReader DataReader = MySQL.GetDataReader(SQL);
                    while (DataReader.Read())
                    {
                        if (DataReader["dep_id"].ToString().Length > 0)
                        {
                            m_intdep_id = Convert.ToInt32(DataReader["dep_id"].ToString());
                        }
                        else
                        {
                            m_intdep_id = -1;
                        }
                        if (DataReader["user_id"].ToString().Length > 0)
                        {
                            m_intuser_id = Convert.ToInt32(DataReader["user_id"].ToString());
                        }
                        else
                        {
                            m_intuser_id = -1;
                        }
                        break;
                    }
                    DataReader.Close();
                }
                catch
                {
                }
            }
            if (m_intcar_id > 0)
            {
                FileLib.DeleteFile("temp.png");
                String StrImageData = "";

                m_TPOld = m_tabMain.SelectedTab;//--2017/02/22 製作返回按鈕功能
                TabPage_Push();//m_StackTPOld.Push(m_TPOld);//--2017/02/22 製作返回按鈕功能

                m_tabSub010100.Parent = m_tabMain;
                initSub010100UI();
                get_show_CarCards(m_intcar_id);//取得車輛的卡片列表
                for(int i=0;i<m_ALUser_ID.Count;i++)//設定駕駛
                {
                    if (Convert.ToInt32(m_ALUser_ID[i].ToString()) == m_intuser_id)
                    {
                        cmdSub010100_02.SelectedIndex = i;
                        break;
                    }
                }
                for (int i = 0; i < m_ALDepartment_ID.Count; i++)//設定部門
                {
                    if (Convert.ToInt32(m_ALDepartment_ID[i].ToString()) == m_intdep_id)
                    {
                        cmdSub010100_01.SelectedIndex = i;
                        break;
                    }
                }

                SQL = String.Format("SELECT * FROM car WHERE id={0};", m_intcar_id);
                MySqlDataReader DataReader = MySQL.GetDataReader(SQL);
                while (DataReader.Read())
                {
                    txtSub010100_01.Text = DataReader["name"].ToString();//名稱
                    txtSub010100_02.Text = DataReader["alias_name"].ToString();//別名
                    txtSub010100_13.Text = DataReader["Administrator_name"].ToString();//保管人姓名
                    txtSub010100_15.Text = DataReader["Administrator_tel"].ToString();//保管人電話
                    txtSub010100_03.Text = DataReader["m_liter"].ToString();//排氣量
                    txtSub010100_04.Text = DataReader["weight"].ToString();//車重
                    try
                    {
                        txtSub010100_07.Value = Convert.ToDateTime(DataReader["factory_date"].ToString());//出廠日期
                    }
                    catch
                    {
                        txtSub010100_07.Value = DateTime.Now;
                    }
                    //DataReader["put_up_date"].ToString();//編成日期
                    //DataReader["break_up_date"].ToString();//解編日期
                    txtSub010100_08.Text = DataReader["asset_no"].ToString();//財產編號
                    txtSub010100_09.Text = DataReader["licence"].ToString();//車牌編號
                    txtSub010100_10.Text = DataReader["parking_space_no"].ToString();//車位號碼
                    txtSub010100_11.Text = DataReader["take_care_tel"].ToString();//廠商電話
                    txtSub010100_12.Text = DataReader["take_care_mobile"].ToString();//廠商行動
                    txtSub010100_14.Text = DataReader["take_care_address"].ToString();//廠商地址
                    dgvSub010100_01.Text = DataReader["note"].ToString();//備註
                    //DataReader["status"].ToString();//狀態

                    StrImageData = DataReader["pic"].ToString();//照片
                    if (StrImageData.Length > 0)
                    {
                        String StrDestFilePath = FileLib.path + "\\temp.png";

                        byte[] data = Convert.FromBase64String(StrImageData);
                        FileLib.CreateFile(StrDestFilePath, data);

                        //--
                        //c# 圖片檔讀取：非鎖定檔方法~http://fecbob.pixnet.net/blog/post/38125005
                        FileStream fs = File.OpenRead(StrDestFilePath); //OpenRead
                        int filelength = 0;
                        filelength = (int)fs.Length; //獲得檔長度
                        Byte[] image = new Byte[filelength]; //建立一個位元組陣列
                        fs.Read(image, 0, filelength); //按位元組流讀取
                        System.Drawing.Image result = System.Drawing.Image.FromStream(fs);
                        fs.Close();
                        //--

                        imgSub010100_01.Image = result;//Image.FromFile(StrDestFilePath);
                    }
                }
                DataReader.Close();

                txtSub010100_09.Enabled = false;//新增車輛時可修改[車牌]欄位但在編輯時禁止修改
                m_tabMain.SelectedTab = m_tabSub010100;

                //--
                //add at 2017/10/12
                m_Sub010100ALInit.Clear();
                m_Sub010100ALRight.Clear();//add at 2017/10/18
                m_Sub010100ALInit.Add(txtSub010100_01.Text);
                m_Sub010100ALInit.Add(txtSub010100_02.Text);
                m_Sub010100ALInit.Add(txtSub010100_03.Text);
                m_Sub010100ALInit.Add(txtSub010100_04.Text);
                m_Sub010100ALInit.Add(txtSub010100_08.Text);
                m_Sub010100ALInit.Add(txtSub010100_09.Text);
                m_Sub010100ALInit.Add(txtSub010100_10.Text);
                m_Sub010100ALInit.Add(txtSub010100_11.Text);
                m_Sub010100ALInit.Add(txtSub010100_12.Text);
                m_Sub010100ALInit.Add(txtSub010100_13.Text);
                m_Sub010100ALInit.Add(txtSub010100_14.Text);
                m_Sub010100ALInit.Add(txtSub010100_15.Text);
                m_Sub010100ALInit.Add(txtSub010100_16.Text);
                m_Sub010100ALInit.Add(cmdSub010100_01.SelectedIndex + "");
                m_Sub010100ALInit.Add(cmdSub010100_02.SelectedIndex + "");
                m_Sub010100ALInit.Add(txtSub010100_07.Value.ToString("yyyy-MM-dd HH:mm"));

                if (StrImageData.Length > 0)
                {
                    m_Sub010100ALInit.Add(StrImageData);
                }

                for (int i = 0; i < dgvSub010100_01.Rows.Count; i++)
                {
                    m_Sub010100ALInit.Add(dgvSub010100_01.Rows[i].Cells[1].Value.ToString());
                    m_Sub010100ALRight.Add(dgvSub010100_01.Rows[i].Cells[1].Value.ToString());//add at 2017/10/18
                }
                //--
            }
        }

        private void butSub0101_02_Click(object sender, EventArgs e)//車輛列表UI-新增
        {
            FileLib.DeleteFile("temp.png");//徹底刪除人員車輛照片暫存檔-2018/04/02防呆用

            m_TPOld = m_tabMain.SelectedTab;//--2017/02/22 製作返回按鈕功能
            TabPage_Push();//m_StackTPOld.Push(m_TPOld);//--2017/02/22 製作返回按鈕功能

            m_tabSub010100.Parent = m_tabMain;
            m_intcar_id = -10;
            initSub010100UI();
            m_tabMain.SelectedTab = m_tabSub010100;

            //--
            //add at 2017/10/12
            m_Sub010100ALInit.Clear();
            m_Sub010100ALInit.Add(txtSub010100_01.Text);
            m_Sub010100ALInit.Add(txtSub010100_02.Text);
            m_Sub010100ALInit.Add(txtSub010100_03.Text);
            m_Sub010100ALInit.Add(txtSub010100_04.Text);
            m_Sub010100ALInit.Add(txtSub010100_08.Text);
            m_Sub010100ALInit.Add(txtSub010100_09.Text);
            m_Sub010100ALInit.Add(txtSub010100_10.Text);
            m_Sub010100ALInit.Add(txtSub010100_11.Text);
            m_Sub010100ALInit.Add(txtSub010100_12.Text);
            m_Sub010100ALInit.Add(txtSub010100_13.Text);
            m_Sub010100ALInit.Add(txtSub010100_14.Text);
            m_Sub010100ALInit.Add(txtSub010100_15.Text);
            m_Sub010100ALInit.Add(txtSub010100_16.Text);
            m_Sub010100ALInit.Add(cmdSub010100_01.SelectedIndex+"");
            m_Sub010100ALInit.Add(cmdSub010100_02.SelectedIndex + "");
            m_Sub010100ALInit.Add(txtSub010100_07.Value.ToString("yyyy-MM-dd HH:mm"));
            //--
        }

        private void butSub0101_03_Click_XX(object sender, EventArgs e)//車輛列表UI-匯入[csv]-寫在按鈕裡
        {
            String StrPath;
            //String StrUID, StrLicence, StrName;
            String SQL = "";
            OpenFileDialog openFileDialog1 = new OpenFileDialog();
            openFileDialog1.Filter = "CSV File|*.csv";
            openFileDialog1.Title = "Open an CSV";
            openFileDialog1.RestoreDirectory = true;
            int intindex = 0;
            if (openFileDialog1.ShowDialog() == DialogResult.OK)
            {
                StrPath = openFileDialog1.FileName.ToString();
                // 建立檔案串流（@ 可取消跳脫字元 escape sequence）
                StreamReader sr = new StreamReader(StrPath);
                String StrTitle = "";
                while (!sr.EndOfStream)// 每次讀取一行，直到檔尾
                {
                    String line = sr.ReadLine();// 讀取文字到 line 變數

                    //--
                    //車輛資料表全部(id和state除外)匯入
                    String Data = "";
                    if (intindex == 0)
                    {
                        StrTitle = line;
                    }
                    else
                    {
                        if ((StrTitle.Length > 0) && (line.Length > 0))
                        {
                            //--
                            //車/人匯入時要有預設部門
                            string[] strs = line.Split(',');
                            String StrName, StrLicence;
                            StrName = strs[0];
                            StrLicence = strs[8];
                            //--

                            Data = "'" + line + "'";
                            Data = Data.Replace(",", "','");
                            /*車/人匯入時要有預設部門
                            SQL += String.Format("INSERT INTO car ({0}) VALUES ({1});", StrTitle, Data);
                            */ 

                            //--
                            //車/人匯入時要有預設部門

                            //---
                            //車輛重複匯入防呆機制
                            //SQL = String.Format("INSERT INTO car ({0}) VALUES ({1});", StrTitle, Data);
                            //bool blnAns = MySQL.InsertUpdateDelete(SQL);//新增資料
                            bool blnAns = false;
                            SQL = String.Format("SELECT id FROM car WHERE licence='{1}';", StrName, StrLicence);
                            MySqlDataReader Readercheck = MySQL.GetDataReader(SQL);//新增資料
                            if (!Readercheck.HasRows)
                            {
                                Readercheck.Close();
                                SQL = String.Format("INSERT INTO car ({0}) VALUES ({1});", StrTitle, Data);
                                blnAns = MySQL.InsertUpdateDelete(SQL);//新增資料
                            }
                            else
                            {
                                Readercheck.Close();
                                blnAns = false;
                            }
                            //---車輛重複匯入防呆機制

                            if (blnAns)
                            {
                                SQL = String.Format("SELECT id FROM car WHERE name='{0}' AND licence='{1}';", StrName, StrLicence);
                                MySqlDataReader DataReader = MySQL.GetDataReader(SQL);//新增資料
                                m_intcar_id = -1;
                                while (DataReader.Read())
                                {
                                    m_intcar_id = Convert.ToInt32(DataReader["id"].ToString());
                                }
                                DataReader.Close();
                                if (m_intcar_id > 0)
                                {
                                    SQL = String.Format("INSERT INTO department_detail (dep_id,state,car_id) VALUES ({0},{1},{2});", -1, 1, m_intcar_id);
                                    MySQL.InsertUpdateDelete(SQL);//新增資料
                                }
                            }
                            //--
                        }
                    }
                    //--

                    /*
                    //--
                    //2017/12/26之前的車輛匯入
                    string[] strs = line.Split(',');
                    if ((strs.Length > 2) && (intindex > 0))
                    {
                        StrUID = strs[0];
                        StrLicence = strs[1];
                        StrName = strs[2];

                        SQL = String.Format("INSERT INTO car (asset_no, licence, name, status, state) VALUES ('{0}','{1}','{2}',1,1);", StrUID, StrLicence, StrName);
                        MySQL.InsertUpdateDelete(SQL);//新增資料
                        SQL = String.Format("SELECT id FROM car WHERE name='{0}' AND licence='{1}';", StrName, StrLicence);
                        MySqlDataReader DataReader = MySQL.GetDataReader(SQL);//新增資料
                        m_intcar_id=-1;
                        while (DataReader.Read())
                        {
                            m_intcar_id = Convert.ToInt32(DataReader["id"].ToString());
                        }
                        DataReader.Close();
                        if (m_intcar_id > 0)
                        {
                            SQL = String.Format("INSERT INTO department_detail (dep_id,state,car_id) VALUES ({0},{1},{2});", 1, 1, m_intcar_id);
                            MySQL.InsertUpdateDelete(SQL);//新增資料
                        }
                    }
                    //--
                    */
                    intindex++;

                    /*車/人匯入時要有預設部門
                    //--
                    //車輛資料表全部(id和state除外)匯入
                    if ((intindex == 50) && (SQL.Length > 0))
                    {
                        MySQL.InsertUpdateDelete(SQL);//新增資料
                        SQL = "";
                        intindex = 1;
                    }
                    //--
                    */ 
                }

                /*車/人匯入時要有預設部門
                //--
                //車輛資料表全部(id和state除外)匯入
                if (SQL.Length > 0)//最後未滿49筆的新增
                {
                    MySQL.InsertUpdateDelete(SQL);//新增資料
                    SQL = "";
                }
                //--
                */ 

                sr.Close();// 關閉串流
                get_show_Car();//取得車輛列表
            }
        }

        //---
        //修改車輛匯入要變成執行序模式+即時進度顯示
        public String m_StrImportCarCSVPath = "";
        public void ImportCarCSV()
        {
            //String StrUID, StrLicence, StrName;
            String SQL = "";
            int intindex = 0;

            // 建立檔案串流（@ 可取消跳脫字元 escape sequence）
            StreamReader sr = new StreamReader(m_StrImportCarCSVPath);
            String StrTitle = "";
            while (!sr.EndOfStream)// 每次讀取一行，直到檔尾
            {
                String line = sr.ReadLine();// 讀取文字到 line 變數

                //--
                //車輛資料表全部(id和state除外)匯入
                String Data = "";
                if (intindex == 0)
                {
                    StrTitle = line;
                }
                else
                {
                    if ((StrTitle.Length > 0) && (line.Length > 0))
                    {
                        //--
                        //車/人匯入時要有預設部門
                        string[] strs = line.Split(',');
                        String StrName, StrLicence;
                        StrName = strs[0];
                        StrLicence = strs[8];
                        //--

                        Data = "'" + line + "'";
                        Data = Data.Replace(",", "','");
                        /*車/人匯入時要有預設部門
                        SQL += String.Format("INSERT INTO car ({0}) VALUES ({1});", StrTitle, Data);
                        */

                        //--
                        //車/人匯入時要有預設部門

                        //---
                        //車輛重複匯入防呆機制
                        //SQL = String.Format("INSERT INTO car ({0}) VALUES ({1});", StrTitle, Data);
                        //bool blnAns = MySQL.InsertUpdateDelete(SQL);//新增資料
                        bool blnAns = false;
                        SQL = String.Format("SELECT id FROM car WHERE licence='{1}';", StrName, StrLicence);
                        MySqlDataReader Readercheck = MySQL.GetDataReader(SQL);//新增資料
                        if (!Readercheck.HasRows)
                        {
                            Readercheck.Close();
                            SQL = String.Format("INSERT INTO car ({0}) VALUES ({1});", StrTitle, Data);
                            blnAns = MySQL.InsertUpdateDelete(SQL);//新增資料
                        }
                        else
                        {
                            Readercheck.Close();
                            blnAns = false;
                        }
                        //---車輛重複匯入防呆機制

                        if (blnAns)
                        {
                            SQL = String.Format("SELECT id FROM car WHERE name='{0}' AND licence='{1}';", StrName, StrLicence);
                            MySqlDataReader DataReader = MySQL.GetDataReader(SQL);//新增資料
                            m_intcar_id = -1;
                            while (DataReader.Read())
                            {
                                m_intcar_id = Convert.ToInt32(DataReader["id"].ToString());
                            }
                            DataReader.Close();
                            if (m_intcar_id > 0)
                            {
                                SQL = String.Format("INSERT INTO department_detail (dep_id,state,car_id) VALUES ({0},{1},{2});", -1, 1, m_intcar_id);
                                MySQL.InsertUpdateDelete(SQL);//新增資料
                            }
                        }
                        //--
                    }
                }
                //--

                /*
                //--
                //2017/12/26之前的車輛匯入
                string[] strs = line.Split(',');
                if ((strs.Length > 2) && (intindex > 0))
                {
                    StrUID = strs[0];
                    StrLicence = strs[1];
                    StrName = strs[2];

                    SQL = String.Format("INSERT INTO car (asset_no, licence, name, status, state) VALUES ('{0}','{1}','{2}',1,1);", StrUID, StrLicence, StrName);
                    MySQL.InsertUpdateDelete(SQL);//新增資料
                    SQL = String.Format("SELECT id FROM car WHERE name='{0}' AND licence='{1}';", StrName, StrLicence);
                    MySqlDataReader DataReader = MySQL.GetDataReader(SQL);//新增資料
                    m_intcar_id=-1;
                    while (DataReader.Read())
                    {
                        m_intcar_id = Convert.ToInt32(DataReader["id"].ToString());
                    }
                    DataReader.Close();
                    if (m_intcar_id > 0)
                    {
                        SQL = String.Format("INSERT INTO department_detail (dep_id,state,car_id) VALUES ({0},{1},{2});", 1, 1, m_intcar_id);
                        MySQL.InsertUpdateDelete(SQL);//新增資料
                    }
                }
                //--
                */
                intindex++;

                /*車/人匯入時要有預設部門
                //--
                //車輛資料表全部(id和state除外)匯入
                if ((intindex == 50) && (SQL.Length > 0))
                {
                    MySQL.InsertUpdateDelete(SQL);//新增資料
                    SQL = "";
                    intindex = 1;
                }
                //--
                */
            }

            /*車/人匯入時要有預設部門
            //--
            //車輛資料表全部(id和state除外)匯入
            if (SQL.Length > 0)//最後未滿49筆的新增
            {
                MySQL.InsertUpdateDelete(SQL);//新增資料
                SQL = "";
            }
            //--
            */

            sr.Close();// 關閉串流
                
        }
        private void butSub0101_03_Click(object sender, EventArgs e)//車輛列表UI-匯入[csv]
        {
            OpenFileDialog openFileDialog1 = new OpenFileDialog();
            openFileDialog1.Filter = "CSV File|*.csv";
            openFileDialog1.Title = "Open an CSV";
            openFileDialog1.RestoreDirectory = true;
            if (openFileDialog1.ShowDialog() == DialogResult.OK)
            {
                m_StrImportCarCSVPath = openFileDialog1.FileName.ToString();
                Animation.createThreadAnimation(butSub0101_03.Text, Animation.Thread_ImportCarCSV);
                get_show_Car();//取得車輛列表
            }
        }
        //---修改車輛匯入要變成執行序模式+即時進度顯示

        private void butSub0101_04_Click(object sender, EventArgs e)//匯出車輛
        {
            String StrPath = "";
            //String Strasset_no, Strlicence, Strname;
            String SQL = "";
            SaveFileDialog saveFileDialog1 = new SaveFileDialog();
            saveFileDialog1.Filter = "CSV File|*.csv";
            saveFileDialog1.Title = "Save an CSV";
            saveFileDialog1.FileName = "car.csv";
            if (saveFileDialog1.ShowDialog() == DialogResult.OK)
            {
                StrPath = saveFileDialog1.FileName.ToString();
                StreamWriter sw = new StreamWriter(StrPath, false, System.Text.Encoding.UTF8);

                //--
                //取消車/人匯出圖片功能和無用欄位
                ArrayList delname = new ArrayList();
                delname.Add("pic");
                delname.Add("put_up_date");
                delname.Add("break_up_date");
                delname.Add("status");
                ArrayList delindex = new ArrayList();
                bool adddata = false;//取消車/人匯出圖片功能和無用欄位
                //--
                //--
                //車輛資料表全部(id和state除外)匯出
                //SQL = "SELECT * FROM car ORDER BY asset_no ASC;";
                SQL = "SELECT d_d.dep_id,c.name,c.alias_name,c.administrator_name,c.administrator_tel,c.m_liter,c.weight,c.factory_date,c.asset_no,c.licence,c.parking_space_no,c.take_care_tel,c.take_care_mobile,c.take_care_address,c.note FROM car AS c,department_detail AS d_d WHERE (d_d.car_id=c.id) AND ((d_d.user_id IS NULL) OR (d_d.user_id=-1)) ORDER BY c.id;";//『車輛』匯出要有部門欄位
                String StrTitle = "";
                MySqlDataReader Reader_Data = MySQL.GetDataReader(SQL);
                for (int i = 0; i < (Reader_Data.VisibleFieldCount - 1); i++)
                {
                    //--
                    //取消車/人匯出圖片功能和無用欄位
                    bool delcheck = false;
                    for (int k = 0; k < delname.Count; k++)
                    {
                        if (Reader_Data.GetName(i) == delname[k].ToString())
                        {
                            delcheck = true;
                            break;
                        }
                    }
                    //--

                    if (!delcheck)//取消車/人匯出圖片功能和無用欄位
                    {
                        if (StrTitle.Length > 0)
                        {
                            StrTitle += ",";
                        }
                        //---
                        //『車輛』匯出要有部門欄位
                        StrTitle += Reader_Data.GetName(i);
                        /*
                        if (i > 0)
                        {
                            StrTitle += Reader_Data.GetName(i);
                        }
                        */ 
                        //---『車輛』匯出要有部門欄位
                    }
                    else//取消車/人匯出圖片功能和無用欄位
                    {
                        delindex.Add(i);
                    }
                }
                sw.WriteLine(StrTitle);

                while (Reader_Data.Read())
                {
                    String Data = "";
                    for (int j = 0; j < (Reader_Data.VisibleFieldCount - 1); j++)
                    {
                        //--
                        //取消車/人匯出圖片功能和無用欄位
                        bool delcheck = false;
                        for (int l = 0; l < delindex.Count; l++)
                        {
                            if (Convert.ToInt32(delindex[l].ToString()) == j)
                            {
                                delcheck = true;
                                break;
                            }
                        }
                        //--
                        if (!delcheck)//取消車/人匯出圖片功能和無用欄位
                        {
                            if (adddata)
                            {
                                Data += ",";
                            }
                            //---
                            //『車輛』匯出要有部門欄位
                            Data += Reader_Data[j].ToString();
                            adddata = true;
                            /*
                            if (j > 0)
                            {
                                Data += Reader_Data[j].ToString();
                                adddata = true;//取消車/人匯出圖片功能和無用欄位
                            }
                            */ 
                            //---『車輛』匯出要有部門欄位
                        }
                    }
                    if (Data.Length > 0)
                    {
                        sw.WriteLine(Data);
                    }
                    adddata = false;//取消車/人匯出圖片功能和無用欄位
                }
                Reader_Data.Close();
                //--

                /*
                //--
                //2017/12/26之前版本的車輛匯出功能
                sw.WriteLine("資產編號,車號,名稱");
                SQL = "SELECT asset_no, licence, name FROM car ORDER BY asset_no ASC;";
                MySqlDataReader Reader_Data = MySQL.GetDataReader(SQL);
                while (Reader_Data.Read())
                {
                    Strasset_no = Reader_Data["asset_no"].ToString();
                    Strlicence = Reader_Data["licence"].ToString();
                    Strname = Reader_Data["name"].ToString();
                    String Data = Strasset_no + "," + Strlicence + "," + Strname;
                    sw.WriteLine(Data);
                }
                Reader_Data.Close();
                //--
                */
                sw.Close();
            }
        }

        private void butSub0101_06_Click(object sender, EventArgs e)//車輛列表UI-全選
        {
            /*
            for (int i = 0; i < dgvSub0101_01.Rows.Count; i++)
            {
                dgvSub0101_01.Rows[i].Cells[0].Value = true;
                dgvSub0101_01.Rows[i].Selected = true;
            }
            */
            dgvSub0101_01.SelectAll();//把所有列表元件的全選/取消全選改成SelectAll()/ClearSelection()
        }

        private void butSub0101_07_Click(object sender, EventArgs e)//車輛列表UI-取消全選
        {
            /*
            for (int i = 0; i < dgvSub0101_01.Rows.Count; i++)
            {
                dgvSub0101_01.Rows[i].Cells[0].Value = false;
                dgvSub0101_01.Rows[i].Selected = false;
            }
            */
            dgvSub0101_01.ClearSelection();//把所有列表元件的全選/取消全選改成SelectAll()/ClearSelection()
        }

        private void butSub0101_08_Click(object sender, EventArgs e)//車輛列表UI-批次處理
        {
            String SQL = "";
            int count = 0;
            ArrayList ALBuf = new ArrayList();
            ALBuf.Clear();
            for (int i = 0; i < dgvSub0101_01.Rows.Count; i++)
            {
                String data = dgvSub0101_01.Rows[i].Cells[0].Value.ToString().ToLower();
                if (data == "true")
                {
                    ALBuf.Add(dgvSub0101_01.Rows[i].Cells[1].Value.ToString());
                }
            }
            switch (cmbSub0101_01.SelectedIndex)
            {
                case 0://Delete Selected
                    for (int i = 0; i < ALBuf.Count; i++)
                    {
                        count++;
                        SQL += String.Format("DELETE FROM car WHERE id={0};", ALBuf[i].ToString());
                        if ((count == 50) && (SQL.Length > 0))
                        {
                            MySQL.InsertUpdateDelete(SQL);
                            SQL = "";
                            count = 0;
                        }
                    }
                    if (SQL.Length > 0)
                    {
                        MySQL.InsertUpdateDelete(SQL);
                    }
                    break;
                /*//車輛批次處理也把Status欄位相關刪除
                case 1://Enable Selected
                    for (int i = 0; i < ALBuf.Count; i++)
                    {
                        count++;
                        SQL += String.Format("UPDATE car SET status=1 WHERE id={0};", ALBuf[i].ToString());
                        if ((count == 50) && (SQL.Length > 0))
                        {
                            MySQL.InsertUpdateDelete(SQL);
                            SQL = "";
                            count = 0;
                        }
                    }
                    if (SQL.Length > 0)
                    {
                        MySQL.InsertUpdateDelete(SQL);
                    }
                    break;
                case 2://Disable Selected
                    for (int i = 0; i < ALBuf.Count; i++)
                    {
                        count++;
                        SQL += String.Format("UPDATE car SET status=0 WHERE id={0};", ALBuf[i].ToString());
                        if ((count == 50) && (SQL.Length > 0))
                        {
                            MySQL.InsertUpdateDelete(SQL);
                            SQL = "";
                            count = 0;
                        }
                    }
                    if (SQL.Length > 0)
                    {
                        MySQL.InsertUpdateDelete(SQL);
                    }
                    break;
                 */ 
            }
            get_show_Car();//取得車輛列表
        }

        private void butSub0101_09_Click_XX(object sender, EventArgs e)//車輛列表UI-搜尋-只在介面上搜尋
        {
            get_show_Car();//取得車輛列表
            ArrayList AL01 = new ArrayList();
            ArrayList AL02 = new ArrayList();
            ArrayList AL03 = new ArrayList();
            ArrayList AL04 = new ArrayList();
            ArrayList AL05 = new ArrayList();
            ArrayList AL06 = new ArrayList();
            ArrayList AL07 = new ArrayList();
            if (txtSub0101_01.Text != "")
            {
                for (int i = 0; i < dgvSub0101_01.Rows.Count; i++)//取的現行UI上卡片列表所有資料
                {
                    AL01.Add(dgvSub0101_01.Rows[i].Cells[1].Value.ToString());
                    AL02.Add(dgvSub0101_01.Rows[i].Cells[2].Value.ToString());
                    AL03.Add(dgvSub0101_01.Rows[i].Cells[3].Value.ToString());
                    AL04.Add(dgvSub0101_01.Rows[i].Cells[4].Value.ToString());
                    AL05.Add(dgvSub0101_01.Rows[i].Cells[5].Value.ToString());
                    AL06.Add(dgvSub0101_01.Rows[i].Cells[6].Value.ToString());
                    AL07.Add(dgvSub0101_01.Rows[i].Cells[7].Value.ToString());
                }
                cleandgvSub0101_01();//清空畫面
                String StrSearch = txtSub0101_01.Text;
                for (int i = 0; i < AL01.Count; i++)
                {
                    if ((AL02[i].ToString().IndexOf(StrSearch) > -1) || (AL03[i].ToString().IndexOf(StrSearch) > -1) || (AL04[i].ToString().IndexOf(StrSearch) > -1) || (AL05[i].ToString().IndexOf(StrSearch) > -1) || (AL06[i].ToString().IndexOf(StrSearch) > -1) || (AL07[i].ToString().IndexOf(StrSearch) > -1))
                    {
                        dgvSub0101_01.Rows.Add(false, AL01[i].ToString(), AL02[i].ToString(), AL03[i].ToString(), AL04[i].ToString(), AL05[i].ToString(), AL06[i].ToString(), AL07[i].ToString());
                    }
                }
            }
        }

        private void butSub0101_09_Click(object sender, EventArgs e)//車輛列表UI-搜尋
        {
            m_SQL_car_condition01 = "";
            m_intCarNowPage = 1;
            if (txtSub0101_01.Text != "")
            {
                m_SQL_car_condition01 = String.Format("WHERE ((c.licence LIKE '%{0}%') OR (c.name LIKE '%{0}%') OR (d.name LIKE '%{0}%') OR (c.asset_no LIKE '%{0}%') OR (c.factory_date LIKE '%{0}%'))", txtSub0101_01.Text);
            }
            get_show_Car();
        }

        private void dgvSub0101_01_SelectionChanged(object sender, EventArgs e)//車輛列表選擇改變事件
        {
            //---
            //列表元件shift全選時最前面的狀態要能自動打勾，反之要能取消
            for (int i = 0; i < dgvSub0101_01.Rows.Count; i++)
            {
                dgvSub0101_01.Rows[i].Cells[0].Value = false;
            }
            for (int j = 0; j < dgvSub0101_01.SelectedRows.Count; j++)
            {
                dgvSub0101_01.SelectedRows[j].Cells[0].Value = true;
            }
            //---列表元件shift全選時最前面的狀態要能自動打勾，反之要能取消

            try
            {
                int index = dgvSub0102_01.SelectedRows[0].Index;//取得被選取的第一列位置
                String Strcar_id = dgvSub0102_01.Rows[index].Cells[1].Value.ToString();
                m_intcar_id = Int32.Parse(Strcar_id);
            }
            catch
            {
            }
        }

        public String m_SQL_car_condition01 = "";
        private void ckbSub0101_01_CheckedChanged(object sender, EventArgs e)//所有車輛列表 SQL延伸過濾選項事件
        {
            m_SQL_car_condition01 = "";
            if (ckbSub0101_01.Checked == true || ckbSub0101_02.Checked == true)
            {
                m_SQL_car_condition01 = "WHERE";
                if (ckbSub0101_01.Checked == true)
                {
                    if (ckbSub0101_02.Checked == true)
                    {//兩個都選等於沒作用
                        m_SQL_car_condition01 += " c.status>=0";
                    }
                    else
                    {
                        m_SQL_car_condition01 += " c.status=1";
                    }
                }
                else
                {
                    if (ckbSub0101_02.Checked == true)
                    {
                        m_SQL_car_condition01 += " c.status=0";
                    }
                    else
                    {//兩個都沒選等於沒作用
                        m_SQL_car_condition01 += " c.status>=0";
                    }
                }
            }
            get_show_Car();//取得車輛列表
        }

        private void butSub0101_11_Click(object sender, EventArgs e)//車輛列表移至第一頁
        {
            m_intCarNowPage = 1;
            get_show_Car();
        }

        private void butSub0101_12_Click(object sender, EventArgs e)//車輛列表移至前一頁
        {
            m_intCarNowPage--;
            if (m_intCarNowPage < 1)
            {
                m_intCarNowPage = 1;
            }
            get_show_Car();
        }

        private void butSub0101_13_Click(object sender, EventArgs e)//車輛列表移至後一頁
        {
            m_intCarNowPage++;
            if (m_intCarNowPage > m_intCarAllPage)
            {
                m_intCarNowPage = m_intCarAllPage;
            }
            get_show_Car();
        }

        private void butSub0101_14_Click(object sender, EventArgs e)//車輛列表移至最後一頁
        {
            m_intCarNowPage = m_intCarAllPage;
            get_show_Car();
        }
        //Sub0101_end
        //Sub010100_start
        private void dgvSub010100_01_DoubleClick(object sender, EventArgs e)//at 2017/09/15
        {
            butSub010100_08.PerformClick();
        }

        private void butSub010100_05_Click(object sender, EventArgs e)//車輛編輯UI中的卡片全選
        {
            /*
            for (int i = 0; i < dgvSub010100_01.Rows.Count; i++)
            {
                dgvSub010100_01.Rows[i].Cells[0].Value = true;
                dgvSub010100_01.Rows[i].Selected = true;
            }
            */
            dgvSub010100_01.SelectAll();//把所有列表元件的全選/取消全選改成SelectAll()/ClearSelection()
        }

        private void butSub010100_06_Click(object sender, EventArgs e)//車輛編輯UI中的卡片取消全選
        {
            /*
            for (int i = 0; i < dgvSub010100_01.Rows.Count; i++)
            {
                dgvSub010100_01.Rows[i].Cells[0].Value = false;
                dgvSub010100_01.Rows[i].Selected = false;
            }
            */
            dgvSub010100_01.ClearSelection();//把所有列表元件的全選/取消全選改成SelectAll()/ClearSelection()
        }

        private void butSub010100_07_Click(object sender, EventArgs e)//車輛編輯UI中的卡片批次處理
        {
            String SQL = "";
            ArrayList ALcard_id = new ArrayList();
            ALcard_id.Clear();
            for (int i = 0; i < dgvSub010100_01.Rows.Count; i++)
            {
                String data = dgvSub010100_01.Rows[i].Cells[0].Value.ToString().ToLower();
                if (data == "true")
                {
                    ALcard_id.Add(dgvSub010100_01.Rows[i].Cells[1].Value.ToString());
                }
            }

            switch (cmbSub010100_01.SelectedIndex)
            {
                case 0:
                    for (int i = 0; i < ALcard_id.Count; i++)
                    {
                        SQL = String.Format("DELETE FROM card_for_user_car WHERE card_id={0} AND car_id={1};", ALcard_id[i].ToString(), m_intcar_id);
                        MySQL.InsertUpdateDelete(SQL);
                    }
                    break;
            }

            get_show_CarCards(m_intcar_id);//取得車輛的卡片列表
        }

        private void butSub010100_08_Click(object sender, EventArgs e)//車輛編輯UI中的編輯卡片
        {
            m_intcard_id = -1;

            try
            {
                int index = dgvSub010100_01.SelectedRows[0].Index;//取得被選取的第一列位置
                String Strcard_id = dgvSub010100_01.Rows[index].Cells[1].Value.ToString();
                m_intcard_id = Int32.Parse(Strcard_id);
            }
            catch
            {
            }

            modifiedCardData();
        }

        private void butSub010100_04_Click(object sender, EventArgs e)//車輛編輯UI-配發卡片
        {
            String SQL = "";
            m_ALCardList.Clear();
            CardList frmCL = new CardList(this, "type=3");//修正『車輛資料管理其中配發卡片的部分，把所有未使用卡片都列出來』BUG
            frmCL.ShowDialog();
            for (int i = 0; i < m_ALCardList.Count; i++)
            {
                String StrCard_id = m_ALCardList[i].ToString();
                SQL = String.Format("INSERT INTO card_for_user_car (card_id,car_id,status,state) VALUES ({0},{1},1,1);", StrCard_id, m_intcar_id);
                MySQL.InsertUpdateDelete(SQL);
            }
            get_show_CarCards(m_intcar_id);//取得車輛的卡片列表
        }

        private void butSub010100_01_Click(object sender, EventArgs e)//車輛編輯UI-清除圖片
        {
            imgSub010100_01.Image = null;
            FileLib.DeleteFile("temp.png");
        }

        private void butSub010100_02_Click(object sender, EventArgs e)//車輛編輯UI-載入圖片
        {
            String StrPath;
            OpenFileDialog openFileDialog1 = new OpenFileDialog();
            openFileDialog1.Filter = "Image File|*.png;*.jpg";
            openFileDialog1.Title = "Open an Image";
            openFileDialog1.RestoreDirectory = true;
            if (openFileDialog1.ShowDialog() == DialogResult.OK)
            {
                imgSub010100_01.Image = null;
                StrPath = openFileDialog1.FileName.ToString();
                String StrDestFilePath = FileLib.path;
                FileLib.DeleteFile("temp.png");
                /*
                if (StrPath.IndexOf(".png") >= 0)
                {
                    StrDestFilePath += "\\" + "temp.png";
                }
                else
                {
                    StrDestFilePath += "\\" + "temp.jpg";
                }
                */

                //---
                //整合縮圖函數
                //StrDestFilePath += "\\" + "temp.png";
                //FileLib.CopyFile(StrPath, StrDestFilePath);
                StrDestFilePath += "\\" + "temp.png";
                FileLib.ImageResize(StrPath, StrDestFilePath, 800);
                //---整合縮圖函數

                //--
                //c# 圖片檔讀取：非鎖定檔方法~http://fecbob.pixnet.net/blog/post/38125005
                FileStream fs = File.OpenRead(StrDestFilePath); //OpenRead
                int filelength = 0;
                filelength = (int)fs.Length; //獲得檔長度
                Byte[] image = new Byte[filelength]; //建立一個位元組陣列
                fs.Read(image, 0, filelength); //按位元組流讀取
                System.Drawing.Image result = System.Drawing.Image.FromStream(fs);
                fs.Close();
                //--

                imgSub010100_01.Image = result;//Image.FromFile(StrDestFilePath);
            }
        }

        private void butSub010100_15_Click(object sender, EventArgs e)//車輛編輯UI-離開
        {
            CarData2DB(false);//add at 2017/10/12
            if ( (m_intcar_id == -1) || CheckUIVarNotChange(m_Sub010100ALInit, m_Sub010100ALData) )//if (m_intcar_id == -1)
            {
                Leave_function();
                get_show_Car();//取得車輛列表
            }
            else
            {
                DialogResult myResult = MessageBox.Show(Language.m_StrControllerMsg00, butSub010100_15.Text, MessageBoxButtons.YesNo, MessageBoxIcon.Question);
                if (myResult == DialogResult.Yes)
                {
                    //--
                    //add at 2017/10/18
                    String SQL = "";
                    String StrCard_id = "";
                    if (m_intcar_id > 0)
                    {
                        SQL = String.Format("DELETE FROM card_for_user_car WHERE car_id={0};", m_intcar_id);
                        MySQL.InsertUpdateDelete(SQL);
                        for (int i = 0; i < m_Sub010100ALRight.Count; i++)
                        {
                            StrCard_id = m_Sub010100ALRight[i].ToString();
                            SQL = String.Format("INSERT INTO card_for_user_car (card_id,car_id,status,state) VALUES ({0},{1},1,1);", StrCard_id, m_intcar_id);
                            MySQL.InsertUpdateDelete(SQL);
                        }
                    }
                    //--

                    Leave_function();
                    get_show_Car();//取得車輛列表
                }
            }
        }
        public bool CarData2DB(bool blnRunSQL = true,int intState = 1)
        {
            bool blnAns = false;
            String SQL="";
            String Strname, Stralias_name, Strpic, Stradministrator_name, Stradministrator_tel, Strm_liter, Strweight, Strfactory_date, Strasset_no, Strlicence, Strparking_space_no, Strtake_care_tel, Strtake_care_mobile, Strtake_care_address, Strnote, Strstatus, Strstate;
            String StrImageData = FileLib.ImageFile2Base64String(FileLib.path + "\\temp.png");
            
            Strname = txtSub010100_01.Text;//名稱
            Stralias_name = txtSub010100_02.Text;//別名
            Stradministrator_name = txtSub010100_13.Text;//保管人姓名
            Stradministrator_tel = txtSub010100_15.Text;//保管人電話
            try
            {
                Strm_liter = Convert.ToString(Convert.ToInt32(txtSub010100_03.Text));//排氣量-int
            }
            catch
            {
                Strm_liter = "0";
            }
            try
            {
                Strweight = Convert.ToString(Convert.ToSingle(txtSub010100_04.Text));//車重-decimal
            }
            catch
            {
                Strweight = "0.0";
            }

            Strfactory_date = txtSub010100_07.Value.ToString("yyyy-MM-dd");//出廠日期
            //Strput_up_date//編成日期
            //Strbreak_up_date//解編日期
            Strasset_no = txtSub010100_08.Text;//財產編號
            Strlicence = txtSub010100_09.Text;//車牌編號
            Strparking_space_no = txtSub010100_10.Text;//車位號碼
            Strtake_care_tel = txtSub010100_11.Text;//廠商電話
            Strtake_care_mobile = txtSub010100_12.Text;//廠商行動
            Strtake_care_address = txtSub010100_14.Text;//廠商地址
            Strnote = txtSub010100_16.Text;//備註
            //Strstatus//狀態
            Strpic = StrImageData;//照片
            if (cmdSub010100_01.SelectedIndex >= 0)
            {
                m_intdep_id = Convert.ToInt32(m_ALDepartment_ID[cmdSub010100_01.SelectedIndex].ToString());
            }
            else
            {
                m_intdep_id = -1;
            }
            if (cmdSub010100_02.SelectedIndex >= 0)
            {
                m_intuser_id = Convert.ToInt32(m_ALUser_ID[cmdSub010100_02.SelectedIndex].ToString());
            }
            else
            {
                m_intuser_id = -1;
            }
            //--
            //add at 2017/10/12
            if (!blnRunSQL)
            {
                m_Sub010100ALData.Clear();
                m_Sub010100ALData.Add(txtSub010100_01.Text);
                m_Sub010100ALData.Add(txtSub010100_02.Text);
                m_Sub010100ALData.Add(txtSub010100_03.Text);
                m_Sub010100ALData.Add(txtSub010100_04.Text);
                m_Sub010100ALData.Add(txtSub010100_08.Text);
                m_Sub010100ALData.Add(txtSub010100_09.Text);
                m_Sub010100ALData.Add(txtSub010100_10.Text);
                m_Sub010100ALData.Add(txtSub010100_11.Text);
                m_Sub010100ALData.Add(txtSub010100_12.Text);
                m_Sub010100ALData.Add(txtSub010100_13.Text);
                m_Sub010100ALData.Add(txtSub010100_14.Text);
                m_Sub010100ALData.Add(txtSub010100_15.Text);
                m_Sub010100ALData.Add(txtSub010100_16.Text);
                m_Sub010100ALData.Add(cmdSub010100_01.SelectedIndex + "");
                m_Sub010100ALData.Add(cmdSub010100_02.SelectedIndex + "");
                m_Sub010100ALData.Add(txtSub010100_07.Value.ToString("yyyy-MM-dd HH:mm"));

                if (StrImageData.Length > 0)
                {
                    m_Sub010100ALData.Add(StrImageData);
                }

                for (int i = 0; i < dgvSub010100_01.Rows.Count; i++)
                {
                    m_Sub010100ALData.Add(dgvSub010100_01.Rows[i].Cells[1].Value.ToString());
                }
                return (!blnRunSQL);
            }
            //--

            //---
            //車輛必填欄位偵測
            if ((Strname != "") && (Strlicence != ""))
            {
                labSub010100_01.ForeColor = Color.Black;
                labSub010100_10.ForeColor = Color.Black;
            }
            else
            {
                if (Strname == "")
                {
                    labSub010100_01.ForeColor = Color.Red;
                }
                else
                {
                    labSub010100_01.ForeColor = Color.Black;
                }

                if (Strlicence == "")
                {
                    labSub010100_10.ForeColor = Color.Red;
                }
                else
                {
                    labSub010100_10.ForeColor = Color.Black;
                }
                blnAns = false;
                return blnAns;
            }
            //---車輛必填欄位偵測

            if (m_intcar_id>0)//修改
            {
                SQL = String.Format("UPDATE car SET name='{0}', alias_name='{1}', pic='{2}', administrator_name='{3}', administrator_tel='{4}', m_liter='{5}', weight='{6}', factory_date='{7}', asset_no='{8}', licence='{9}', parking_space_no='{10}', take_care_tel='{11}', take_care_mobile='{12}', take_care_address='{13}', note='{14}', state={15} WHERE id={16};",
                                                    Strname, Stralias_name, Strpic, Stradministrator_name, Stradministrator_tel, Strm_liter, Strweight, Strfactory_date, Strasset_no, Strlicence, Strparking_space_no, Strtake_care_tel, Strtake_care_mobile, Strtake_care_address, Strnote, intState, m_intcar_id);

                //SQL += String.Format("UPDATE department_detail SET dep_id={0},user_id={1},state={2} WHERE car_id={3};", m_intdep_id, m_intuser_id, intState, m_intcar_id);
                //--
                //車輛資料表全部(id和state除外)匯入
                bool check = false;
                MySqlDataReader DataReader = MySQL.GetDataReader(String.Format("SELECT * FROM department_detail WHERE car_id={0};", m_intcar_id));
                while (DataReader.Read())
                {
                    check = true;
                }
                DataReader.Close();
                if (check == true)
                {
                    SQL += String.Format("UPDATE department_detail SET dep_id={0},user_id={1},state={2} WHERE car_id={3};", m_intdep_id, m_intuser_id, intState, m_intcar_id);
                }
                else
                {
                    SQL += String.Format("INSERT INTO department_detail (dep_id,user_id,car_id,state) VALUES ({0},{1},{2},{3});", m_intdep_id, m_intuser_id, m_intcar_id, intState);
                }
                //--

                blnAns = MySQL.InsertUpdateDelete(SQL);
            }
            else//新增
            {
                //---
                //車輛UI[車牌]欄位判斷不能重複
                SQL = String.Format("SELECT id FROM car WHERE licence='{0}';", Strlicence);
                MySqlDataReader ReaderCheck = MySQL.GetDataReader(SQL);//新增資料
                while (ReaderCheck.HasRows)
                {
                    txtSub010100_09.Text = "";
                    labSub010100_10.ForeColor = Color.Red;
                    ReaderCheck.Close();
                    blnAns = false;
		            return blnAns;	
                }
                ReaderCheck.Close();
                //---車輛UI[車牌]欄位判斷不能重複
                SQL = String.Format("INSERT INTO car (name, alias_name, pic, administrator_name, administrator_tel, m_liter, weight, factory_date, asset_no, licence, parking_space_no, take_care_tel, take_care_mobile, take_care_address, note, state) VALUES ('{0}','{1}','{2}','{3}','{4}','{5}','{6}','{7}','{8}','{9}','{10}','{11}','{12}','{13}','{14}',{15});",
                                                      Strname, Stralias_name, Strpic, Stradministrator_name, Stradministrator_tel, Strm_liter, Strweight, Strfactory_date, Strasset_no, Strlicence, Strparking_space_no, Strtake_care_tel, Strtake_care_mobile, Strtake_care_address, Strnote, intState);
                blnAns = MySQL.InsertUpdateDelete(SQL);
                SQL = String.Format("SELECT id FROM car WHERE name='{0}' AND licence='{1}';", Strname, Strlicence);
                MySqlDataReader DataReader = MySQL.GetDataReader(SQL);//新增資料
                m_intcar_id = -10;
                while (DataReader.Read())
                {
                    m_intcar_id = Convert.ToInt32(DataReader["id"].ToString());
                }
                DataReader.Close();
                if (m_intcar_id > 0)
                {
                    SQL = String.Format("UPDATE card_for_user_car SET car_id={0},state={1} WHERE car_id=-10;", m_intcar_id, intState);
                    SQL += String.Format("INSERT INTO department_detail (dep_id,user_id,car_id,state) VALUES ({0},{1},{2},{3});", m_intdep_id, m_intuser_id, m_intcar_id, intState);
                    blnAns = MySQL.InsertUpdateDelete(SQL);
                }

            }

            if (blnAns)
            {
                m_intcar_id = -1;
            }

            return blnAns;
        }

        private void butSub010100_13_Click(object sender, EventArgs e)//車輛編輯UI-儲存設定
        {
            if (CarData2DB())
            {
                get_show_Car();//取得車輛列表
                Leave_function();
            }

        }

        private void butSub010100_16_Click(object sender, EventArgs e)//車輛管理，駕駛下拉選單無空白欄位
        {
            cmdSub010100_02.SelectedIndex = -1;
        }

        private void dgvSub010100_01_SelectionChanged(object sender, EventArgs e)
        {
            //---
            //列表元件shift全選時最前面的狀態要能自動打勾，反之要能取消
            for (int i = 0; i < dgvSub010100_01.Rows.Count; i++)
            {
                dgvSub010100_01.Rows[i].Cells[0].Value = false;
            }
            for (int j = 0; j < dgvSub010100_01.SelectedRows.Count; j++)
            {
                dgvSub010100_01.SelectedRows[j].Cells[0].Value = true;
            }
            //---列表元件shift全選時最前面的狀態要能自動打勾，反之要能取消
        }
        //Sub010100_end
        //Sub0103_start

        //--
        //修改編修部門UI~製作下拉式選單
        private void cmbSub0103_01_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (cmbSub0103_01.SelectedIndex > -1)
            {
                m_intselectdepid = Convert.ToInt32(m_ALDepartment_ID[cmbSub0103_01.SelectedIndex].ToString());
                m_intleftdepid = m_intselectdepid;
                initlvSub0103_01_ALL();//initlvSub0103_01();//按照目前所選部門，顯示在他下面的所有部門
            }
        }

        private void cmbSub0103_02_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (cmbSub0103_02.SelectedIndex > -1)
            {
                m_intselectdepid = Convert.ToInt32(m_ALDepartment_ID[cmbSub0103_02.SelectedIndex].ToString());
                m_intrightdepid = m_intselectdepid;
                initlvSub0103_02_ALL();//initlvSub0103_02();//按照目前所選部門，顯示在他下面的人員+車輛
            }
        }
        //--

        private void butSub0103_07_Click(object sender, EventArgs e)//匯入部門資訊
        {
            bool blnDepItem00 = false;
            String StrPath;
            String Strid, StrName, Strunit;
            String SQL = "";
            OpenFileDialog openFileDialog1 = new OpenFileDialog();
            openFileDialog1.Filter = "CSV File|*.csv";
            openFileDialog1.Title = "Open an CSV";
            openFileDialog1.RestoreDirectory = true;
            int intindex = 0;// add 2017/12/10
            if (openFileDialog1.ShowDialog() == DialogResult.OK)
            {
                //MySQL.ClearTable("department");//清空原本資料表
                StrPath = openFileDialog1.FileName.ToString();
                // 建立檔案串流（@ 可取消跳脫字元 escape sequence）
                StreamReader sr = new StreamReader(StrPath);
                while (!sr.EndOfStream)// 每次讀取一行，直到檔尾
                {
                    String line = sr.ReadLine();// 讀取文字到 line 變數

                    string[] strs = line.Split(',');
                    if ((strs.Length > 2) && (intindex > 0))
                    {
                        Strid = strs[0];
                        StrName = strs[1];
                        Strunit = strs[2];
                        if (Strunit == "")
                        {
                            Strunit = "0";
                        }
                        /*
                        //2017/12/26 之前都是一口氣匯入全部部門
                        SQL += String.Format("INSERT INTO department (id, name, unit, state) VALUES ({0}, '{1}',{2},1);", Strid, StrName, Strunit);
                        */ 
                        //--
                        //修正部門匯入方法，從一口氣匯入變成單一匯入，預防一筆失敗全部失敗的狀況
                        SQL = String.Format("INSERT INTO department (id, name, unit, state) VALUES ({0}, '{1}',{2},1);", Strid, StrName, Strunit);
                        MySQL.InsertUpdateDelete(SQL);//新增資料程式
                        //--
                    }
                    intindex++;
                }
                /*
                //2017/12/26 之前都是一口氣匯入全部部門
                if (SQL.Length > 0)
                {
                    MySQL.InsertUpdateDelete(SQL);//新增資料程式
                }
                */
                sr.Close();// 關閉串流

                SQL = String.Format("SELECT id FROM department WHERE id={0};", -1);
                MySqlDataReader DataReader = MySQL.GetDataReader(SQL);//新增資料
                while (DataReader.Read())
                {
                    blnDepItem00 = true;
                }
                DataReader.Close();
                if (blnDepItem00 == true)//更新
                {
                    SQL = String.Format("UPDATE department SET name='{0}' WHERE id=-1;", Language.m_StrDepItem00);
                }
                else//新增
                {
                    SQL = String.Format("INSERT INTO department (id, name, unit, descript, state) VALUES (-1, '{0}', 0, NULL, 1);", Language.m_StrDepItem00);
                }
                MySQL.InsertUpdateDelete(SQL);//新增資料程式

                inittvSub0103_01();//重載部門資料
            }
        }

        private void butSub0103_08_Click(object sender, EventArgs e)//匯出部門資訊
        {
            String StrPath = "";
            String Strid, StrName, Strunit;
            String SQL = "";
            SaveFileDialog saveFileDialog1 = new SaveFileDialog();
            saveFileDialog1.Filter = "CSV File|*.csv";
            saveFileDialog1.Title = "Save an CSV";
            saveFileDialog1.FileName = "department.csv";
            if (saveFileDialog1.ShowDialog() == DialogResult.OK)
            {
                StrPath = saveFileDialog1.FileName.ToString();
                StreamWriter sw = new StreamWriter(StrPath, false,System.Text.Encoding.UTF8);
                sw.WriteLine("部門編號,部門名稱,上層部門編號");
                SQL = "SELECT id, name, unit FROM department ORDER BY id ASC;";
                MySqlDataReader Reader_Data = MySQL.GetDataReader(SQL);
                while (Reader_Data.Read())
                {
                    Strid = Reader_Data["id"].ToString();
                    StrName = Reader_Data["name"].ToString();
                    Strunit = Reader_Data["unit"].ToString();
                    String Data = Strid + "," + StrName + "," + Strunit;
                    sw.WriteLine(Data);
                }
                Reader_Data.Close();
                sw.Close();
            }
        }

        private void tvSub0103_01_AfterSelect(object sender, TreeViewEventArgs e)//部門選擇
        {
            Tree_Node tmp_Node = (Tree_Node)(tvSub0103_01.SelectedNode);
            //MessageBox.Show(tmp_Node.Text+"\ndep_id="+tmp_Node.m_id);
            if (tmp_Node != null)
            {
                m_intselectdepid = tmp_Node.m_id;//抓取所選部門ID值
                if (m_intWorkArea == 0)//0->left,1->right
                {
                    //--
                    //修改編修部門UI~製作下拉式選單
                    for (int i = 0; i < m_ALDepartment_ID.Count; i++)
                    {
                        if ( Convert.ToInt32(m_ALDepartment_ID[i].ToString()) == m_intselectdepid )
                        {
                            cmbSub0103_01.SelectedIndex = i;
                            break;
                        }
                    }
                    /*
                    m_intleftdepid = m_intselectdepid;
                    m_Nodeleft = null;
                    m_Nodeleft = tmp_Node;
                    labSub0103_05.Text = "Now：" + tmp_Node.Text;
                    initlvSub0103_01_ALL();//initlvSub0103_01();//按照目前所選部門，顯示在他下面的所有部門
                    */ 
                    //--
                }
                else
                {
                    //--
                    //修改編修部門UI~製作下拉式選單
                    for (int i = 0; i < m_ALDepartment_ID.Count; i++)
                    {
                        if (Convert.ToInt32(m_ALDepartment_ID[i].ToString()) == m_intselectdepid)
                        {
                            cmbSub0103_02.SelectedIndex = i;
                            break;
                        }
                    }
                    /*
                    m_intrightdepid = m_intselectdepid;
                    m_Noderight = null;
                    m_Noderight = tmp_Node;
                    labSub0103_06.Text = "Now：" + tmp_Node.Text;
                    initlvSub0103_02_ALL();//initlvSub0103_02();//按照目前所選部門，顯示在他下面的人員+車輛
                    */ 
                    //--
                }
                tmp_Node.Expand();//展開目前所選部門的樹
            }
            else
            {
                m_intselectdepid = -2;
            }
        }

        private void butSub0103_01_Click(object sender, EventArgs e)//部門新增
        {
            //Tree_Node tmp = (Tree_Node)tvSub0103_01.SelectedNode;
            if (m_Nodeleft != null)
            {
                AddDepartment AddDepartment = new AddDepartment(m_Nodeleft);
                AddDepartment.ShowDialog();

                inittvSub0103_01();
                tvSub0103_01.SelectedNode = null;
                m_intleftdepid = m_Nodeleft.m_id;
                initlvSub0103_01_ALL();//initlvSub0103_01();//子部門顯示元件初始化
                //initlvSub0103_02_ALL();//initlvSub0103_02();//部門下人員+車輛顯示元件初始化    
            }
        }

        private void butSub0103_02_Click(object sender, EventArgs e)//部門刪除
        {
            int index = -1;
            String SQL = "";
            if (lvSub0103_01.SelectedItems.Count > 0)
            {
                foreach (ListViewItem item in lvSub0103_01.SelectedItems)
                {
                    index = item.Index;
                    bool blndep = false;
                    Tree_Node tmp_node = (Tree_Node)m_ALlvSub0103_01[index];
                    SQL = String.Format("SELECT COUNT(id) AS num FROM department WHERE id={0} AND name='{1}';", tmp_node.m_id, tmp_node.Text);//確定是部門
                    MySqlDataReader ReaderdData = MySQL.GetDataReader(SQL);
                    while (ReaderdData.Read())
                    {
                        if (Convert.ToInt32(ReaderdData["num"].ToString()) > 0)
                        {
                            blndep = true;
                        }
                    }
                    ReaderdData.Close();

                    if (tmp_node != null && blndep == true)
                    {
                        int num0 = 0, num1 = 0;
                        SQL = String.Format("SELECT COUNT(id) AS num FROM department WHERE unit={0};", tmp_node.m_id);//不能是別人的上層部門
                        MySqlDataReader ReaderdData00 = MySQL.GetDataReader(SQL);
                        while (ReaderdData00.Read())
                        {
                            num0 = Convert.ToInt32(ReaderdData00["num"].ToString());
                        }
                        ReaderdData00.Close();

                        SQL = String.Format("SELECT COUNT(dep_id) AS num FROM department_detail WHERE dep_id={0};", tmp_node.m_id);//該部門內不能有人員或車輛
                        MySqlDataReader ReaderdData01 = MySQL.GetDataReader(SQL);
                        while (ReaderdData01.Read())
                        {
                            num1 = Convert.ToInt32(ReaderdData01["num"].ToString());
                        }
                        ReaderdData01.Close();

                        if ((num0 + num1) <= 0)
                        {
                            SQL = String.Format("DELETE FROM department WHERE id={0};", tmp_node.m_id);
                            MySQL.InsertUpdateDelete(SQL);
                            labSub0103_05.Text = "";
                            //MessageBox.Show("OK");
                        }
                        else
                        {
                            MessageBox.Show(Language.m_StrDeleteDepartmentMsg01, Language.m_StrDeleteDepartmentMsg00, MessageBoxButtons.OK, MessageBoxIcon.Error);
                        }
                    }
                }
            }
            else//只選根部門
            {
                int num0 = 0, num1 = 0;
                SQL = String.Format("SELECT COUNT(id) AS num FROM department WHERE unit={0};", m_intleftdepid);//不能是別人的上層部門
                MySqlDataReader ReaderdData00 = MySQL.GetDataReader(SQL);
                while (ReaderdData00.Read())
                {
                    num0 = Convert.ToInt32(ReaderdData00["num"].ToString());
                }
                ReaderdData00.Close();

                SQL = String.Format("SELECT COUNT(dep_id) AS num FROM department_detail WHERE dep_id={0};", m_intleftdepid);//該部門內不能有人員或車輛
                MySqlDataReader ReaderdData01 = MySQL.GetDataReader(SQL);
                while (ReaderdData01.Read())
                {
                    num1 = Convert.ToInt32(ReaderdData01["num"].ToString());
                }
                ReaderdData01.Close();

                if ((num0 + num1) <= 0)
                {
                    SQL = String.Format("DELETE FROM department WHERE id={0};", m_intleftdepid);
                    MySQL.InsertUpdateDelete(SQL);
                    labSub0103_05.Text = "";
                    //MessageBox.Show("OK");
                }
                else
                {
                    MessageBox.Show(Language.m_StrDeleteDepartmentMsg01, Language.m_StrDeleteDepartmentMsg00, MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
            }
            inittvSub0103_01();
            tvSub0103_01.SelectedNode = null;
            m_intleftdepid = m_Nodeleft.m_id;
            initlvSub0103_01_ALL();//initlvSub0103_01();//子部門顯示元件初始化
            /*
            int index = -1;
            String SQL = "";
            if (m_intselectdepid > 0)
            {
                if (lvSub0103_01.SelectedItems.Count > 0)//有選到子部門
                {
                    foreach (ListViewItem item in lvSub0103_01.SelectedItems)
                    {
                        index = item.Index;
                        Tree_Node tmp_node = (Tree_Node)m_ALlvSub0103_01[index];
                        if (tmp_node != null)
                        {
                            int num0=0, num1=0;
                            SQL = String.Format("SELECT COUNT(id) AS num FROM department WHERE unit={0};", tmp_node.m_id);//不能是別人的上層部門
                            MySqlDataReader ReaderdData00 = MySQL.GetDataReader(SQL);
                            while (ReaderdData00.Read())
                            {
                                num0 = Convert.ToInt32(ReaderdData00["num"].ToString());
                            }
                            ReaderdData00.Close();

                            SQL = String.Format("SELECT COUNT(dep_id) AS num FROM department_detail WHERE dep_id={0};", tmp_node.m_id);//該部門內不能有人員或車輛
                            MySqlDataReader ReaderdData01 = MySQL.GetDataReader(SQL);
                            while (ReaderdData01.Read())
                            {
                                num1 = Convert.ToInt32(ReaderdData01["num"].ToString());
                            }
                            ReaderdData01.Close();

                            if ((num0 + num1) <= 0)
                            {
                                SQL = String.Format("DELETE FROM department WHERE id={0};", tmp_node.m_id);
                                MySQL.InsertUpdateDelete(SQL);
                                //MessageBox.Show("OK");
                            }
                            else
                            {
                                MessageBox.Show(Language.m_StrDeleteDepartmentMsg01, Language.m_StrDeleteDepartmentMsg00, MessageBoxButtons.OK, MessageBoxIcon.Error);
                            }
                        }
                    }
                }
                else//只選根部門
                {
                    int num0=0, num1=0;
                    SQL = String.Format("SELECT COUNT(id) AS num FROM department WHERE unit={0};", m_intselectdepid);//不能是別人的上層部門
                    MySqlDataReader ReaderdData00 = MySQL.GetDataReader(SQL);
                    while (ReaderdData00.Read())
                    {
                        num0 = Convert.ToInt32(ReaderdData00["num"].ToString());
                    }
                    ReaderdData00.Close();

                    SQL = String.Format("SELECT COUNT(dep_id) AS num FROM department_detail WHERE dep_id={0};", m_intselectdepid);//該部門內不能有人員或車輛
                    MySqlDataReader ReaderdData01 = MySQL.GetDataReader(SQL);
                    while (ReaderdData01.Read())
                    {
                        num1 = Convert.ToInt32(ReaderdData01["num"].ToString());
                    }
                    ReaderdData01.Close();

                    if ((num0 + num1) <= 0)
                    {
                        SQL = String.Format("DELETE FROM department WHERE id={0};", m_intselectdepid);
                        MySQL.InsertUpdateDelete(SQL);
                        //MessageBox.Show("OK");
                    }
                    else
                    {
                        MessageBox.Show(Language.m_StrDeleteDepartmentMsg01, Language.m_StrDeleteDepartmentMsg00, MessageBoxButtons.OK, MessageBoxIcon.Error);
                    }
                }
            }
            inittvSub0103_01();
            initlvSub0103_01_ALL();//initlvSub0103_01();//子部門顯示元件初始化
            initlvSub0103_02_ALL();//initlvSub0103_02();//部門下人員+車輛顯示元件初始化 
            */
 
        }

        private void butSub0103_05_Click(object sender, EventArgs e)//部門刪除
        {
            int index = -1;
            String SQL = "";
            if (lvSub0103_02.SelectedItems.Count > 0)
            {
                foreach (ListViewItem item in lvSub0103_02.SelectedItems)
                {
                    index = item.Index;
                    bool blndep = false;
                    Tree_Node tmp_node = (Tree_Node)m_ALlvSub0103_02[index];
                    SQL = String.Format("SELECT COUNT(id) AS num FROM department WHERE id={0} AND name='{1}';", tmp_node.m_id, tmp_node.Text);//確定是部門
                    MySqlDataReader ReaderdData = MySQL.GetDataReader(SQL);
                    while (ReaderdData.Read())
                    {
                        if (Convert.ToInt32(ReaderdData["num"].ToString()) > 0)
                        {
                            blndep = true;
                        }
                    }
                    ReaderdData.Close();

                    if (tmp_node != null && blndep == true)
                    {
                        int num0 = 0, num1 = 0;
                        SQL = String.Format("SELECT COUNT(id) AS num FROM department WHERE unit={0};", tmp_node.m_id);//不能是別人的上層部門
                        MySqlDataReader ReaderdData00 = MySQL.GetDataReader(SQL);
                        while (ReaderdData00.Read())
                        {
                            num0 = Convert.ToInt32(ReaderdData00["num"].ToString());
                        }
                        ReaderdData00.Close();

                        SQL = String.Format("SELECT COUNT(dep_id) AS num FROM department_detail WHERE dep_id={0};", tmp_node.m_id);//該部門內不能有人員或車輛
                        MySqlDataReader ReaderdData01 = MySQL.GetDataReader(SQL);
                        while (ReaderdData01.Read())
                        {
                            num1 = Convert.ToInt32(ReaderdData01["num"].ToString());
                        }
                        ReaderdData01.Close();

                        if ((num0 + num1) <= 0)
                        {
                            SQL = String.Format("DELETE FROM department WHERE id={0};", tmp_node.m_id);
                            MySQL.InsertUpdateDelete(SQL);
                            labSub0103_06.Text = "";
                            //MessageBox.Show("OK");
                        }
                        else
                        {
                            MessageBox.Show(Language.m_StrDeleteDepartmentMsg01, Language.m_StrDeleteDepartmentMsg00, MessageBoxButtons.OK, MessageBoxIcon.Error);
                        }
                    }
                }
            }
            else//只選根部門
            {
                int num0 = 0, num1 = 0;
                SQL = String.Format("SELECT COUNT(id) AS num FROM department WHERE unit={0};", m_intrightdepid);//不能是別人的上層部門
                MySqlDataReader ReaderdData00 = MySQL.GetDataReader(SQL);
                while (ReaderdData00.Read())
                {
                    num0 = Convert.ToInt32(ReaderdData00["num"].ToString());
                }
                ReaderdData00.Close();

                SQL = String.Format("SELECT COUNT(dep_id) AS num FROM department_detail WHERE dep_id={0};", m_intrightdepid);//該部門內不能有人員或車輛
                MySqlDataReader ReaderdData01 = MySQL.GetDataReader(SQL);
                while (ReaderdData01.Read())
                {
                    num1 = Convert.ToInt32(ReaderdData01["num"].ToString());
                }
                ReaderdData01.Close();

                if ((num0 + num1) <= 0)
                {
                    SQL = String.Format("DELETE FROM department WHERE id={0};", m_intrightdepid);
                    MySQL.InsertUpdateDelete(SQL);
                    labSub0103_06.Text = "";
                    //MessageBox.Show("OK");
                }
                else
                {
                    MessageBox.Show(Language.m_StrDeleteDepartmentMsg01, Language.m_StrDeleteDepartmentMsg00, MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
            }
            inittvSub0103_01();
            tvSub0103_01.SelectedNode = null;
            m_intrightdepid = m_Noderight.m_id;
            initlvSub0103_02_ALL();//initlvSub0103_01();//子部門顯示元件初始化
            /*
            int index = -1;
            foreach (ListViewItem item in lvSub0103_02.SelectedItems)
            {
                //String SQL = "";
                index = item.Index;
                Tree_Node tmp_node = (Tree_Node)m_ALlvSub0103_02[index];
                if (tmp_node != null)
                {
                    m_TPOld = m_tabMain.SelectedTab;//--2017/02/22 製作返回按鈕功能
                    TabPage_Push();//m_StackTPOld.Push(m_TPOld);//--2017/02/22 製作返回按鈕功能

                    if (tmp_node.m_tree_level == 0)//0->人
                    {
                        //SQL = String.Format("DELETE FROM department_detail WHERE (user_id={0}) AND (car_id IS NULL) AND (dep_id={1});",tmp_node.m_id,tmp_node.m_unit);
                        m_tabSub0100.Parent = m_tabMain;
                        initSub0100UI(true);
                        m_tabMain.SelectedTab = m_tabSub0100;
                    }
                    else//1->車輛
                    {
                        //SQL = String.Format("DELETE FROM department_detail WHERE (car_id={0}) AND (dep_id={1});", tmp_node.m_id, tmp_node.m_unit);
                        m_tabSub0101.Parent = m_tabMain;
                        initSub0101UI(true);
                        m_tabMain.SelectedTab = m_tabSub0101;	
                    }
                    //MySQL.InsertUpdateDelete(SQL);
                }
            }
            initlvSub0103_02_ALL();//initlvSub0103_02();//部門下人員+車輛顯示元件初始化  
            */
        }

        public static int m_intSelectAddItem = -1;
        private void butSub0103_04_Click(object sender, EventArgs e)//部門刪除
        {
            //Tree_Node tmp = (Tree_Node)tvSub0103_01.SelectedNode;
            if (m_Noderight != null)
            {
                AddDepartment AddDepartment = new AddDepartment(m_Noderight);
                AddDepartment.ShowDialog();

                inittvSub0103_01();
                tvSub0103_01.SelectedNode = null;
                m_intrightdepid = m_Noderight.m_id;
                //initlvSub0103_01_ALL();//initlvSub0103_01();//子部門顯示元件初始化
                initlvSub0103_02_ALL();//initlvSub0103_02();//部門下人員+車輛顯示元件初始化    
            }
            /*
            m_intSelectAddItem = -1;
            SelectAddItem SelectAddItem = new SelectAddItem();
            SelectAddItem.ShowDialog();
            if (m_intSelectAddItem > -1)
            {
                m_TPOld = m_tabMain.SelectedTab;//--2017/02/22 製作返回按鈕功能
                TabPage_Push();//m_StackTPOld.Push(m_TPOld);//--2017/02/22 製作返回按鈕功能
                if (m_intSelectAddItem == 0)//人
                {
                    m_tabSub0100.Parent = m_tabMain;
                    initSub0100UI(true);
                    m_tabMain.SelectedTab = m_tabSub0100;
                }
                else//車輛
                {
                    m_tabSub0101.Parent = m_tabMain;
                    initSub0101UI(true);
                    m_tabMain.SelectedTab = m_tabSub0101;	
                }
            }
            */ 
        }

        private void lvSub0103_01_MouseDown(object sender, MouseEventArgs e)//選擇工作區域
        {
            m_intWorkArea = 0;
            changeWorkArea();
        }

        private void lvSub0103_02_MouseDown(object sender, MouseEventArgs e)//選擇工作區域
        {
            m_intWorkArea = 1;
            changeWorkArea();
        }

        private void lvSub0103_01_MouseUp(object sender, MouseEventArgs e)//修改子部門名稱
        {
            if (e.Button == MouseButtons.Right && lvSub0103_01.SelectedItems.Count > 0)
            {
                ListViewItem LVI;
                LVI = lvSub0103_01.SelectedItems[0];//紀錄被拖放的元件
                modifyDepartmentName((Tree_Node)m_ALlvSub0103_01[LVI.Index + (m_intLV01NowPage - 1) * 1000]);//修正部門管理分頁後執行拖拉分類時會因為頁碼問題造成所選與結果對應不上的問題
            }
        }

        private void lvSub0103_02_MouseUp(object sender, MouseEventArgs e)//修改子部門名稱
        {
            if (e.Button == MouseButtons.Right && lvSub0103_02.SelectedItems.Count > 0)
            {
                ListViewItem LVI;
                LVI = lvSub0103_02.SelectedItems[0];//紀錄被拖放的元件
                modifyDepartmentName((Tree_Node)m_ALlvSub0103_02[LVI.Index + (m_intLV02NowPage - 1) * 1000]);//修正部門管理分頁後執行拖拉分類時會因為頁碼問題造成所選與結果對應不上的問題
            }
        }
        
        private void lvSub0103_01_ItemDrag(object sender, ItemDragEventArgs e)//來源進行拖放
        {
            string s;
            ListViewItem LVI;
            LVI = lvSub0103_01.SelectedItems[0];//紀錄被拖放的元件
            s = LVI.Text;
            m_NodeItemDrag = (Tree_Node)m_ALlvSub0103_01[LVI.Index + (m_intLV01NowPage - 1) * 1000];//修正部門管理分頁後執行拖拉分類時會因為頁碼問題造成所選與結果對應不上的問題

            //--
            //修改編修部門UI~ 可支援一個以上的元素拖拉移動
            m_ALItemDrag.Clear();
            for (int i = 0; i < lvSub0103_01.SelectedItems.Count; i++)
            {
                LVI = lvSub0103_01.SelectedItems[i];
                m_NodeItemDrag = (Tree_Node)m_ALlvSub0103_01[LVI.Index + (m_intLV01NowPage - 1) * 1000];//修正部門管理分頁後執行拖拉分類時會因為頁碼問題造成所選與結果對應不上的問題
                m_ALItemDrag.Add(m_NodeItemDrag);
            }
            //--

            DragDropEffects dde1 = DoDragDrop(s,DragDropEffects.All);
            /*
            if (dde1 == DragDropEffects.All)//確定已經離開的判斷
            {
                lvSub0103_01.Items.Remove(LVI);
            }
            */ 
        }

        private void lvSub0103_02_ItemDrag(object sender, ItemDragEventArgs e)//來源進行拖放
        {
            string s;
            ListViewItem LVI;
            LVI = lvSub0103_02.SelectedItems[0];//紀錄被拖放的元件
            s = LVI.Text;
            m_NodeItemDrag = (Tree_Node)m_ALlvSub0103_02[LVI.Index + (m_intLV02NowPage - 1) * 1000];//修正部門管理分頁後執行拖拉分類時會因為頁碼問題造成所選與結果對應不上的問題

            //--
            //修改編修部門UI~ 可支援一個以上的元素拖拉移動
            m_ALItemDrag.Clear();
            for (int i = 0; i < lvSub0103_02.SelectedItems.Count; i++)
            {
                LVI = lvSub0103_02.SelectedItems[i];
                m_NodeItemDrag = (Tree_Node)m_ALlvSub0103_02[LVI.Index + (m_intLV02NowPage - 1) * 1000];//修正部門管理分頁後執行拖拉分類時會因為頁碼問題造成所選與結果對應不上的問題
                m_ALItemDrag.Add(m_NodeItemDrag);
            }
            //--

            DragDropEffects dde1 = DoDragDrop(s, DragDropEffects.All);

            /*
            if (dde1 == DragDropEffects.All)//確定已經離開的判斷
            {
                lvSub0103_02.Items.Remove(LVI);
            }
            */ 
        }

        private void lvSub0103_01_DragDrop(object sender, DragEventArgs e)
        {
            if (e.Data.GetDataPresent(DataFormats.StringFormat))//確定以拖拉到目的區域
            {
                if (m_intleftdepid > -2 && (m_intleftdepid != m_NodeItemDrag.m_unit))
                {
                    //MessageBox.Show(m_NodeItemDrag.Text + "_1");

                    //--
                    //修改編修部門UI~ 可支援一個以上的元素拖拉移動
                    //ListViewDragDrop2SQL(m_intleftdepid);
                    ArrayListDragDrop2SQL(m_intleftdepid);
                    //--
                }
            }
        }

        private void lvSub0103_01_DragOver(object sender, DragEventArgs e)
        {
            e.Effect = DragDropEffects.All;
        }

        private void lvSub0103_02_DragDrop(object sender, DragEventArgs e)
        {
            if (e.Data.GetDataPresent(DataFormats.StringFormat))//確定以拖拉到目的區域
            {
                if (m_intrightdepid > -2 && (m_intrightdepid != m_NodeItemDrag.m_unit))
                {
                    //MessageBox.Show(m_NodeItemDrag.Text + "_2");

                    //--
                    //修改編修部門UI~ 可支援一個以上的元素拖拉移動
                    //ListViewDragDrop2SQL(m_intrightdepid);
                    ArrayListDragDrop2SQL(m_intrightdepid);
                    //--
                }
            }
        }

        private void lvSub0103_02_DragOver(object sender, DragEventArgs e)
        {
            e.Effect = DragDropEffects.All;
        }

        private void lvSub0103_01_MouseDoubleClick(object sender, MouseEventArgs e)//進入子目錄
        {
            if (lvSub0103_01.SelectedItems.Count > 0)
            {
                String SQL;
                bool blndep = false;
                ListViewItem LVI;
                LVI = lvSub0103_01.SelectedItems[0];
                Tree_Node tmp_Node = (Tree_Node)m_ALlvSub0103_01[LVI.Index + (m_intLV01NowPage - 1) * 1000];//修正部門管理分頁後執行拖拉分類時會因為頁碼問題造成所選與結果對應不上的問題
                SQL = String.Format("SELECT COUNT(id) AS num FROM department WHERE id={0} AND name='{1}';", tmp_Node.m_id, tmp_Node.Text);
                MySqlDataReader ReaderdData = MySQL.GetDataReader(SQL);
                while (ReaderdData.Read())
                {
                    if (Convert.ToInt32(ReaderdData["num"].ToString()) > 0)
                    {
                        blndep = true;
                    }
                }
                ReaderdData.Close();
                if (blndep == true)
                {
                    m_intleftdepid = tmp_Node.m_id;
                    labSub0103_05.Text = "Now：" + tmp_Node.Text;
                    //--
                    //修改編修部門UI~製作下拉式選單
                    //initlvSub0103_01_ALL();
                    cmbSub0103_01.SelectedIndex = tmp_Node.m_id;
                    //--
                }
            }
        }

        private void lvSub0103_02_MouseDoubleClick(object sender, MouseEventArgs e)//進入子目錄
        {
            if (lvSub0103_02.SelectedItems.Count > 0)
            {
                String SQL;
                bool blndep = false;
                ListViewItem LVI;
                LVI = lvSub0103_02.SelectedItems[0];
                Tree_Node tmp_Node = (Tree_Node)m_ALlvSub0103_02[LVI.Index + (m_intLV02NowPage - 1) * 1000];//修正部門管理分頁後執行拖拉分類時會因為頁碼問題造成所選與結果對應不上的問題
                SQL = String.Format("SELECT COUNT(id) AS num FROM department WHERE id={0} AND name='{1}';", tmp_Node.m_id, tmp_Node.Text);
                MySqlDataReader ReaderdData = MySQL.GetDataReader(SQL);
                while (ReaderdData.Read())
                {
                    if (Convert.ToInt32(ReaderdData["num"].ToString()) > 0)
                    {
                        blndep = true;
                    }
                }
                ReaderdData.Close();
                if (blndep == true)
                {
                    m_intrightdepid = tmp_Node.m_id;
                    labSub0103_06.Text = "Now：" + tmp_Node.Text;
                    
                    //--
                    //修改編修部門UI~製作下拉式選單
                    //initlvSub0103_02_ALL();
                    cmbSub0103_02.SelectedIndex = tmp_Node.m_id;
                    //--
                }
            }
        }

        private void tvSub0103_01_MouseUp(object sender, MouseEventArgs e)//修改部門名稱
        {
            if (e.Button == MouseButtons.Right)
            {
                if (tvSub0103_01.SelectedNode != null)
                {
                    Tree_Node tmp_Node = (Tree_Node)tvSub0103_01.SelectedNode;
                    modifyDepartmentName(tmp_Node);
                }
                else
                {
                    AddDepartment AddDepartment = new AddDepartment();
                    AddDepartment.ShowDialog();
                    inittvSub0103_01();
                }
                m_intleftdepid = -2;
                m_intrightdepid = -2;
                labSub0103_05.Text = "";
                labSub0103_06.Text = "";
                initcmbSub010000_All();//修正『建立新部門，下拉無新的選項』BUG [部門更名也一併修改]
                initlvSub0103_01_ALL();
                initlvSub0103_02_ALL();
            }
            else
            {
                tvSub0103_01.SelectedNode = null;//修正區域門區群組建立後無法跳至最上層，除非新增區域在取消 [部門UI也一併修改]
            }
        }

        private void tvSub0103_01_MouseDown(object sender, MouseEventArgs e)
        {

        }

        private void butSub0103_11_Click(object sender, EventArgs e)//左側部門新增
        {
            Tree_Node tmp_node = (Tree_Node)(tvSub0103_01.SelectedNode);
            AddDepartment AddDepartment;
            if (tmp_node != null)
            {
                AddDepartment = new AddDepartment(tmp_node);
            }
            else
            {
                AddDepartment = new AddDepartment();
            }
            AddDepartment.ShowDialog();

            inittvSub0103_01();
            initcmbSub010000_All();//修正『建立新部門，下拉無新的選項』BUG
            tvSub0103_01.SelectedNode = null;
        }

        private void butSub0103_12_Click(object sender, EventArgs e)//左側部門刪除
        {
            Tree_Node tmp_node = (Tree_Node)(tvSub0103_01.SelectedNode);
            //MessageBox.Show(tmp_Node.Text+"\ndep_id="+tmp_Node.m_id);
            if (tmp_node != null)
            {
                String SQL;
                bool blndep = false;

                SQL = String.Format("SELECT COUNT(id) AS num FROM department WHERE id={0} AND name='{1}';", tmp_node.m_id, tmp_node.Text);//確定是部門
                MySqlDataReader ReaderdData = MySQL.GetDataReader(SQL);
                while (ReaderdData.Read())
                {
                    if (Convert.ToInt32(ReaderdData["num"].ToString()) > 0)
                    {
                        blndep = true;
                        break;
                    }
                }
                ReaderdData.Close();

                if (tmp_node != null && blndep == true)
                {
                    int num0 = 0, num1 = 0;
                    SQL = String.Format("SELECT COUNT(id) AS num FROM department WHERE unit={0};", tmp_node.m_id);//不能是別人的上層部門
                    MySqlDataReader ReaderdData00 = MySQL.GetDataReader(SQL);
                    while (ReaderdData00.Read())
                    {
                        num0 = Convert.ToInt32(ReaderdData00["num"].ToString());
                    }
                    ReaderdData00.Close();

                    SQL = String.Format("SELECT COUNT(dep_id) AS num FROM department_detail WHERE dep_id={0};", tmp_node.m_id);//該部門內不能有人員或車輛
                    MySqlDataReader ReaderdData01 = MySQL.GetDataReader(SQL);
                    while (ReaderdData01.Read())
                    {
                        num1 = Convert.ToInt32(ReaderdData01["num"].ToString());
                    }
                    ReaderdData01.Close();

                    if ((num0 + num1) <= 0)
                    {
                        SQL = String.Format("DELETE FROM department WHERE id={0};", tmp_node.m_id);
                        MySQL.InsertUpdateDelete(SQL);
                        inittvSub0103_01();
                        initcmbSub010000_All();//修正『建立新部門，下拉無新的選項』BUG
                        tvSub0103_01.SelectedNode = null;

                        m_intleftdepid = -2;
                        m_intrightdepid = -2;
                        labSub0103_05.Text = "";
                        labSub0103_06.Text = "";
                        initlvSub0103_01_ALL();
                        initlvSub0103_02_ALL();
                    }
                    else
                    {
                        MessageBox.Show(Language.m_StrDeleteDepartmentMsg01, Language.m_StrDeleteDepartmentMsg00, MessageBoxButtons.OK, MessageBoxIcon.Error);
                    }
                }
            }
        }

        private void butSub0103_13_Click(object sender, EventArgs e)//移至第一頁
        {
            m_intLV01NowPage = 1;
            showData2PageLV01();
        }

        private void butSub0103_14_Click(object sender, EventArgs e)//移至前一頁
        {
            m_intLV01NowPage--;
            if (m_intLV01NowPage < 1)
            {
                m_intLV01NowPage = 1;
            }
            showData2PageLV01();
        }

        private void butSub0103_15_Click(object sender, EventArgs e)//移至後一頁
        {
            m_intLV01NowPage++;
            if (m_intLV01NowPage > m_intLV01AllPage)
            {
                m_intLV01NowPage = m_intLV01AllPage;
            }
            showData2PageLV01();
        }

        private void butSub0103_16_Click(object sender, EventArgs e)//移至最後頁
        {
            m_intLV01NowPage = m_intLV01AllPage;
            showData2PageLV01();
        }

        private void butSub0103_17_Click(object sender, EventArgs e)//移至第一頁
        {
            m_intLV02NowPage = 1;
            showData2PageLV02();
        }

        private void butSub0103_18_Click(object sender, EventArgs e)//移至前一頁
        {
            m_intLV02NowPage--;
            if (m_intLV02NowPage < 1)
            {
                m_intLV02NowPage = 1;
            }
            showData2PageLV02();
        }

        private void butSub0103_19_Click(object sender, EventArgs e)//移至後一頁
        {
            m_intLV02NowPage++;
            if (m_intLV02NowPage > m_intLV02AllPage)
            {
                m_intLV02NowPage = m_intLV02AllPage;
            }
            showData2PageLV02();
        }

        private void butSub0103_20_Click(object sender, EventArgs e)//移至後一頁
        {
            m_intLV02NowPage = m_intLV02AllPage;
            showData2PageLV02();
        }
        
        //Sub0103_end
        //Sub000200_start
        private void dgvSub000200_01_DoubleClick(object sender, EventArgs e)//at 2017/09/15
        {
            butSub000200_11.PerformClick();
        }

        private void butSub000200_03_Click(object sender, EventArgs e)//新增
        {
            String SQL = "";
            String name, enable, param_enable, level, available_date_start, available_date_end;
            if (txtSub000200_01.Text.Length > 0)
            {
                labSub000200_02.ForeColor = Color.Black;

                name = txtSub000200_01.Text;
                enable = Convert.ToInt32(ckbSub000200_01.Checked) + "";
                param_enable = Convert.ToInt32(ckbSub000200_02.Checked) + "";
                //--
                level = "-1";
                if (rdbSub000200_01.Checked)
                {
                    level = "0";
                }
                if (rdbSub000200_02.Checked)
                {
                    level = "1";
                }
                if (rdbSub000200_03.Checked)
                {
                    level = "2";
                }
                if (rdbSub000200_04.Checked)
                {
                    level = "3";
                }
                //--
                available_date_start = adpSub000200_01.Value.ToString("yyyy-MM-dd HH:mm");
                available_date_end = adpSub000200_02.Value.ToString("yyyy-MM-dd HH:mm");

                SQL = String.Format(@"INSERT INTO door_group (name,enable,param_enable,level,available_date_start,available_date_end,state) VALUES ('{0}',{1},{2},{3},'{4}','{5}',1);", name, enable, param_enable, level, available_date_start, available_date_end);
                MySQL.InsertUpdateDelete(SQL);

                SQL = String.Format(@"SELECT id FROM door_group WHERE name='{0}' ORDER BY id DESC;", name);
                MySqlDataReader DataReader = MySQL.GetDataReader(SQL);
                int id = -1;
                while (DataReader.Read())
                {
                    id = Int32.Parse(DataReader["id"].ToString());
                }
                DataReader.Close();
                if (id > -1)
                {
                    SQL = String.Format(@"DELETE FROM door_group_detail WHERE door_group_id={0};", id);
                    MySQL.InsertUpdateDelete(SQL);

                    getTreeView(tvmSub000200_01);
                    if (m_ALdoor_group_detail.Count > 0)
                    {
                        for (int i = 0; i < m_ALdoor_group_detail.Count; i++)
                        {
                            SQL = String.Format(@"INSERT INTO door_group_detail (door_group_id,area_id,door_id,floor_id,state) VALUES({0});", (id + "," + m_ALdoor_group_detail[i].ToString() + ",1"));
                            MySQL.InsertUpdateDelete(SQL);
                        }
                    }

                }

                initdgvSub000200_01();
                initdgvSub0002_01();

                //--

                initSub0002UI();
                m_intDB2LeftSub000200_id = -1;
                Leave_function();
            }
            else
            {
                labSub000200_02.ForeColor = Color.Red;
                MessageBox.Show(Language.m_StrbutSub000200_03Msg01, butSub000200_03.Text, MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void butSub000200_02_Click(object sender, EventArgs e)//修改儲存
        {
            String SQL = "";
            String name, enable, param_enable, level, available_date_start, available_date_end;
            if (txtSub000200_01.Text.Length > 0)
            {
                labSub000200_02.ForeColor = Color.Black;

                name = txtSub000200_01.Text;
                enable = Convert.ToInt32(ckbSub000200_01.Checked) + "";
                param_enable = Convert.ToInt32(ckbSub000200_02.Checked) + "";
                //--
                level = "-1";
                if (rdbSub000200_01.Checked)
                {
                    level = "0";
                }
                if (rdbSub000200_02.Checked)
                {
                    level = "1";
                }
                if (rdbSub000200_03.Checked)
                {
                    level = "2";
                }
                if (rdbSub000200_04.Checked)
                {
                    level = "3";
                }
                //--
                available_date_start = adpSub000200_01.Value.ToString("yyyy-MM-dd HH:mm");
                available_date_end = adpSub000200_02.Value.ToString("yyyy-MM-dd HH:mm");
                if (m_intDB2LeftSub000200_id > -1)
                {
                    SQL = String.Format(@"UPDATE door_group SET name='{0}',enable={1},param_enable={2},level={3},available_date_start='{4}',available_date_end='{5}',state=1 WHERE id={6};", name, enable, param_enable, level, available_date_start, available_date_end, m_intDB2LeftSub000200_id);
                    MySQL.InsertUpdateDelete(SQL);

                    SQL = String.Format(@"DELETE FROM door_group_detail WHERE door_group_id={0};", m_intDB2LeftSub000200_id);
                    MySQL.InsertUpdateDelete(SQL);

                    getTreeView(tvmSub000200_01);
                    if (m_ALdoor_group_detail.Count > 0)
                    {
                        for (int i = 0; i < m_ALdoor_group_detail.Count; i++)
                        {
                            SQL = String.Format(@"INSERT INTO door_group_detail (door_group_id,area_id,door_id,floor_id,state) VALUES({0});", (m_intDB2LeftSub000200_id + "," + m_ALdoor_group_detail[i].ToString() + ",1"));
                            MySQL.InsertUpdateDelete(SQL);
                        }
                    }

                    initdgvSub000200_01();
                    initdgvSub0002_01();
                }

                //--
                initSub0002UI();
                m_intDB2LeftSub000200_id = -1;
                Leave_function();
            }
            else
            {
                labSub000200_02.ForeColor = Color.Red;
                MessageBox.Show(Language.m_StrbutSub000200_02Msg01, butSub000200_02.Text, MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void butSub000200_06_Click(object sender, EventArgs e)//全選
        {
            for (int i = 0; i < tvmSub000200_01.Nodes.Count; i++)
            {
                Tree_Node tmp = ((Tree_Node)tvmSub000200_01.Nodes[i]);
                tmp.Checked = true;
                AreaTree_NodeFun.SetChildNodeCheckedState(tmp, tmp.Checked);
            }
        }

        private void butSub000200_07_Click(object sender, EventArgs e)//取消全選
        {
            for (int i = 0; i < tvmSub000200_01.Nodes.Count; i++)
            {
                Tree_Node tmp = ((Tree_Node)tvmSub000200_01.Nodes[i]);
                tmp.Checked = false;
                AreaTree_NodeFun.SetChildNodeCheckedState(tmp, tmp.Checked);
            }
        }

        public int m_intdgvSub000200_01_id = -1;
        private void dgvSub000200_01_SelectionChanged(object sender, EventArgs e)
        {
            try
            {
                int index = dgvSub000200_01.SelectedRows[0].Index;//取得被選取的第一列位置
                String Strid = dgvSub000200_01.Rows[index].Cells[1].Value.ToString();
                m_intdgvSub000200_01_id = Int32.Parse(Strid);
            }
            catch
            {
            }
        }

        public bool m_blnrunbutSub000200_09 = false;
        private void butSub000200_09_Click(object sender, EventArgs e)//設置區域
        {
            Tree_Node tmp = ((Tree_Node)tvmSub000200_01.SelectedNode);//得到目前被選擇的節點
            if ((tmp != null) && (tmp.m_tree_level < 2))
            {
                m_TPOld = m_tabMain.SelectedTab;//--2017/02/22 製作返回按鈕功能

                TabPage_Push();//m_StackTPOld.Push(m_TPOld);//--2017/02/22 製作返回按鈕功能
                m_tabSub0001.Parent = m_tabMain;//隱藏系統一開始時，沒用過的分頁，當要顯示時必須先指定父物件-2017/03/02
                m_tabMain.SelectedTab = m_tabSub0001;//2017/02/08 add
                initSub0001UI();
                opentvmSub0001_02_node(tmp.m_id, tmp.m_unit);//呼叫遞迴展開對應的節點達到顯示正確的位置
                tvmSub000200_01.SelectedNode = null;//防止下次有殘留值
                txtSub0001_01.Focus();//--2017/03/30 頁面切換後，指定該頁面特定元件取的焦點(Focus)
                m_blnrunbutSub000200_09 = true;
            }
            else
            {
                if (tmp != null)//因為不是可以反映的節點，所以要把UI設定回選擇狀態提醒User 原本的選擇
                {
                    tvmSub000200_01.SelectedNode = tmp;//設定選擇ID
                    tmp.BackColor = SystemColors.Highlight;//設定是選擇的UI狀態
                    tmp.ForeColor = SystemColors.HighlightText;//設定是選擇的UI狀態
                    m_blnrunbutSub000200_09 = false;
                }
            }
        }

        public bool m_blnrunbutSub000200_10 = false;
        private void butSub000200_10_Click(object sender, EventArgs e)//檢視門區設定
        {
            //---
            //m_tabSub000200 ~ initSelectDoorArray(...)
            //Tree_Node tmp = ((Tree_Node)tvmSub000200_01.SelectedNode);//得到目前被選擇的節點
            Tree_Node tmp = null;
            initSelectDoorArray(3);
            if (m_ALDoorObj.Count > 1)
            {
                tmp = ((Tree_Node)m_ALDoorObj[0]);
            }
            else
            {
                tmp = ((Tree_Node)tvmSub000200_01.SelectedNode);
            }
            //---m_tabSub000200 ~ initSelectDoorArray(...)

            
            if ((tmp != null) && (tmp.m_tree_level == 2))//m_tree_level = 2表示門
            {
                //--
                //modified 2017/10/27
                String Strid = "-1", Strunit = "-1", Strname = "", Strdoor_number = "-1";
                String SQL = String.Format("SELECT ce.door_number AS door_number,c.name AS name,d.controller_id AS controller_id FROM door AS d,controller_extend AS ce,controller AS c WHERE (d.controller_id=ce.controller_sn) AND (d.controller_id=c.sn) AND (d.id={0});", tmp.m_id);
                MySqlDataReader Readerd_id = MySQL.GetDataReader(SQL);
                while (Readerd_id.Read())
                {
                    Strid = Readerd_id["controller_id"].ToString();
                    Strunit = "-1";
                    Strname = Readerd_id["name"].ToString();
                    Strdoor_number = Readerd_id["door_number"].ToString();
                    break;
                }
                Readerd_id.Close();

                //---
                //m_tabSub000200 ~ initSelectDoorArray(...)
                if (Strdoor_number == "-1")
                {
                    Strid = "" + tmp.m_id;
                    Strunit = "" + tmp.m_unit;
                    Strname = tmp.Text;
                    Strdoor_number = tmp.m_data;
                }
                //---m_tabSub000200 ~ initSelectDoorArray(...)

                if (Int32.Parse(Strdoor_number) < 99)
                {
                    ShowtabSub000100UI(tmp.m_id, tmp.m_unit, Int32.Parse(Strdoor_number), tmp.Text);
                }
                else
                {
                    ShowtabSub000100UI(Int32.Parse(Strid), Int32.Parse(Strunit), Int32.Parse(Strdoor_number), Strname);
                }
                /*
                m_TPOld = m_tabMain.SelectedTab;//--2017/02/22 製作返回按鈕功能

                TabPage_Push();//m_StackTPOld.Push(m_TPOld);//--2017/02/22 製作返回按鈕功能

                initSub000100UI(tmp.Text);//為了讓門區抬頭加上門區名稱 at 2017/09/19
                //txtSub000100_01.Text = tmp.Text;//抓取門區名稱
                String Strdoorid = tmp.m_id.ToString();//抓取門區在資料庫的索引值
                m_StrSub000100door_id = Strdoorid;

                String SQL = "";
                int door_number = 0;
                int number = 0;
                bool blnDoor = true;//add 2017/10/23
                SQL = String.Format("SELECT d.controller_door_index AS d_num,c_e.door_number AS num FROM door AS d,controller_extend AS c_e WHERE (d.id={0}) AND (d.controller_id=c_e.controller_sn);", Strdoorid);//SQL = String.Format("SELECT controller_door_index AS d_num FROM door WHERE id={0};", Strdoorid);
                MySqlDataReader Readerd_num = MySQL.GetDataReader(SQL);
                while (Readerd_num.Read())
                {
                    door_number = Convert.ToInt32(Readerd_num["d_num"].ToString());
                    number = Convert.ToInt32(Readerd_num["num"].ToString());

                    //--
                    //add 2017/10/23
                    if (number > 99)
                    {
                        blnDoor = false;
                        labSub000100.Text = Language.m_StrTabPageTag000101 + "-" + tmp.Text;//新增引數為了顯示門區名 at 2017/09/19
                        m_tabSub000100.Text = Language.m_StrTabPageTag000101;
                    }
                    else
                    {
                        blnDoor = true;
                        labSub000100.Text = Language.m_StrTabPageTag000100 + "-" + tmp.Text;//新增引數為了顯示門區名 at 2017/09/19
                        m_tabSub000100.Text = Language.m_StrTabPageTag000100;
                    }
                    txtSub000100_01.Text = tmp.Text;//抓取門區名稱
                    //--

                    break;
                }
                Readerd_num.Close();
                Sub000100_initUIVar(door_number, number, true);//把UI變數初始化

                m_blnSub000100modified = false;//修正 無法紀錄 door_extend 的BUG at 2017/09/19
                SQL = String.Format("SELECT base,pass,open,anti_de,detect,button,anti_co,overtime,violent,pass_mode,auto_mode FROM door_extend WHERE door_id={0};", Strdoorid);
                MySqlDataReader DataReader = MySQL.GetDataReader(SQL);
                while (DataReader.Read())
                {
                    m_blnSub000100modified = true;
                    m_StrBase = DataReader["base"].ToString();// text 'xxxxx,0,0'
                    m_StrPass = DataReader["pass"].ToString();// text '0,0,0,0,0-0-0,1-0-0'
                    m_StrOpen = DataReader["open"].ToString();// text '0,0,0,0'
                    m_StrAnti_de = DataReader["anti_de"].ToString();// text '0,0,0,0'
                    m_StrDetect = DataReader["detect"].ToString();// text '0,0,0,0'
                    m_StrButton = DataReader["button"].ToString();// text '0,0,0,0,0'
                    m_StrAnti_co = DataReader["anti_co"].ToString();// text 'xxxxx,0,0,0,0,0'
                    m_StrOvertime = DataReader["overtime"].ToString();// text '0,0,0,0,0,0,0'
                    m_StrViolent = DataReader["violent"].ToString();// text '0,0,0,0,0'
                    m_StrPass_mode = DataReader["pass_mode"].ToString();// text '0-0,0-0,0-0,0-0,0,0,0,0:0~0:0,0:0~0:0,0:0~0:0,0:0~0:0,0:0~0:0,0:0~0:0,0:0~0:0,0:0~0:0'
                    m_StrAuto_mode = DataReader["auto_mode"].ToString();// text '0,0,0,0:0~0:0,0:0~0:0,0:0~0:0,0:0~0:0,0:0~0:0,0:0~0:0,0:0~0:0,0:0~0:0'
                    break;
                }
                DataReader.Close();
                Sub000100_setUIValue();

                if (true)
                {
                    m_tabSub000100.Parent = m_tabMain;//門區設定UI顯示 at 2017/07/03
                    m_tabMain.SelectedTab = m_tabSub000100;//門區設定UI顯示 at 2017/07/03

                    m_Sub000100ALInit.Clear();//add at 2017/10/06
                    m_Sub000100ALInit.Add(m_StrBase);//add at 2017/10/06
                    m_Sub000100ALInit.Add(m_StrPass);//add at 2017/10/06
                    m_Sub000100ALInit.Add(m_StrOpen);//add at 2017/10/06
                    m_Sub000100ALInit.Add(m_StrAnti_de);//add at 2017/10/06
                    m_Sub000100ALInit.Add(m_StrDetect);//add at 2017/10/06
                    m_Sub000100ALInit.Add(m_StrButton);//add at 2017/10/06
                    m_Sub000100ALInit.Add(m_StrAnti_co);//add at 2017/10/06
                    m_Sub000100ALInit.Add(m_StrOvertime);//add at 2017/10/06
                    m_Sub000100ALInit.Add(m_StrViolent);//add at 2017/10/06
                    m_Sub000100ALInit.Add(m_StrPass_mode);//add at 2017/10/06
                    m_Sub000100ALInit.Add(m_StrAuto_mode);//add at 2017/10/06

                    //--
                    //add 2017/10/23
                    #if !(Delta_Tool)
                        SwitchDoorElevatorUI(blnDoor);
                    #endif
                    //--
                }
                else
                {
                    m_tabSub000101.Parent = m_tabMain;//電梯設定UI顯示 at 2017/07/03
                    m_tabMain.SelectedTab = m_tabSub000101;//電梯設定UI顯示 at 2017/07/03
                }
                */
                //--
            }
            else
            {
                if (tmp != null)//因為不是可以反映的節點，所以要把UI設定回選擇狀態提醒User 原本的選擇
                {
                    tvmSub000200_01.SelectedNode = tmp;//設定選擇ID
                    tmp.BackColor = SystemColors.Highlight;//設定是選擇的UI狀態
                    tmp.ForeColor = SystemColors.HighlightText;//設定是選擇的UI狀態
                    m_blnrunbutSub000200_09 = false;
                }
            }
        }

        private void butSub000200_11_Click(object sender, EventArgs e)//編輯
        {
            try
            {
                int index = dgvSub000200_01.SelectedRows[0].Index;//取得被選取的第一列位置
                String Strid = dgvSub000200_01.Rows[index].Cells[1].Value.ToString();
                m_intdgvSub000200_01_id = Int32.Parse(Strid);
            }
            catch
            {
            }

            if (m_intdgvSub000200_01_id > 0)
            {
                DB2LeftSub000200UI(m_intdgvSub000200_01_id);

                m_Sub000200ALInit.Clear();//add at 2017/10/06
                m_Sub000200ALInit.Add(txtSub000200_01.Text);//add at 2017/10/06
                m_Sub000200ALInit.Add(ckbSub000200_01.Checked.ToString());//add at 2017/10/06
                m_Sub000200ALInit.Add(ckbSub000200_02.Checked.ToString());//add at 2017/10/06
                m_Sub000200ALInit.Add(adpSub000200_01.Value.ToString("yyyy-MM-dd HH:mm"));//add at 2017/10/06
                m_Sub000200ALInit.Add(adpSub000200_02.Value.ToString("yyyy-MM-dd HH:mm"));//add at 2017/10/06
                m_Sub000200ALInit.Add(rdbSub000200_01.Checked.ToString());//add at 2017/10/06
                m_Sub000200ALInit.Add(rdbSub000200_02.Checked.ToString());//add at 2017/10/06
                m_Sub000200ALInit.Add(rdbSub000200_03.Checked.ToString());//add at 2017/10/06
                m_Sub000200ALInit.Add(rdbSub000200_04.Checked.ToString());//add at 2017/10/06
                getTreeView(tvmSub000200_01);//add at 2017/10/06
                for (int i = 0; i < m_ALdoor_group_detail.Count; i++)
                {
                    m_Sub000200ALInit.Add(m_ALdoor_group_detail[i].ToString());//add at 2017/10/06
                }
            }
        }

        private void butSub000200_12_Click(object sender, EventArgs e)//新增
        {
            initLeftSub000200UI();

            m_Sub000200ALInit.Clear();//add at 2017/10/06
            m_Sub000200ALInit.Add(txtSub000200_01.Text);//add at 2017/10/06
            m_Sub000200ALInit.Add(ckbSub000200_01.Checked.ToString());//add at 2017/10/06
            m_Sub000200ALInit.Add(ckbSub000200_02.Checked.ToString());//add at 2017/10/06
            m_Sub000200ALInit.Add(adpSub000200_01.Value.ToString("yyyy-MM-dd HH:mm"));//add at 2017/10/06
            m_Sub000200ALInit.Add(adpSub000200_02.Value.ToString("yyyy-MM-dd HH:mm"));//add at 2017/10/06
            m_Sub000200ALInit.Add(rdbSub000200_01.Checked.ToString());//add at 2017/10/06
            m_Sub000200ALInit.Add(rdbSub000200_02.Checked.ToString());//add at 2017/10/06
            m_Sub000200ALInit.Add(rdbSub000200_03.Checked.ToString());//add at 2017/10/06
            m_Sub000200ALInit.Add(rdbSub000200_04.Checked.ToString());//add at 2017/10/06
            //克服元件在點選展開後就無法實現設定子節點連動改變父節點的BUG at 2017/07/12 21:06 ~實驗用程式碼 MessageBox.Show(tvmSub000200_01._bCheckBoxesVisible.ToString()+"\n"+ tvmSub000200_01._bPreventCheckEvent.ToString()+"\n"+ tvmSub000200_01._bUseTriState.ToString());
        }

        private void butSub000200_16_Click(object sender, EventArgs e)//全選
        {
            /*
            for (int i = 0; i < dgvSub000200_01.Rows.Count; i++)
            {
                dgvSub000200_01.Rows[i].Cells[0].Value = true;
                dgvSub000200_01.Rows[i].Selected = true;
            }
            */
            dgvSub000200_01.SelectAll();//把所有列表元件的全選/取消全選改成SelectAll()/ClearSelection()
        }

        private void butSub000200_17_Click(object sender, EventArgs e)//取消全選
        {
            /*
            for (int i = 0; i < dgvSub000200_01.Rows.Count; i++)
            {
                dgvSub000200_01.Rows[i].Cells[0].Value = false;
                dgvSub000200_01.Rows[i].Selected = false;
            }
            */
            dgvSub000200_01.ClearSelection();//把所有列表元件的全選/取消全選改成SelectAll()/ClearSelection()
        }

        private void butSub000200_18_Click(object sender, EventArgs e)//批次執行
        {
            ArrayList ALSN = new ArrayList();
            ALSN.Clear();
            for (int i = 0; i < dgvSub000200_01.Rows.Count; i++)
            {
                String data = dgvSub000200_01.Rows[i].Cells[0].Value.ToString().ToLower();//抓取DataGridView欄位資料
                if (data == "true")
                {
                    ALSN.Add(dgvSub000200_01.Rows[i].Cells[1].Value.ToString());//抓 ID
                }
            }
            String SQL = "";
            switch (cmbSub000200_01.SelectedIndex)
            {
                case 0:
                    SQL = "";
                    for (int i = 0; i < ALSN.Count; i++)
                    {
                        SQL += "UPDATE door_group SET enable = 1,state=1 WHERE (id = " + ALSN[i].ToString() + ");";
                    }

                    break;
                case 1:
                    SQL = "";
                    for (int i = 0; i < ALSN.Count; i++)
                    {
                        SQL += "UPDATE door_group SET enable = 0,state=1 WHERE (id = " + ALSN[i].ToString() + ");";
                    }
                    break;
                case 2:
                    SQL = "";
                    for (int i = 0; i < ALSN.Count; i++)
                    {
                        SQL += String.Format("DELETE FROM door_group WHERE id={0};DELETE FROM door_group_detail WHERE door_group_id={0};", ALSN[i].ToString());
                    }
                    break;
            }
            MySQL.InsertUpdateDelete(SQL);//新增資料程式

            initdgvSub000200_01();
            initdgvSub0002_01();

            initLeftSub000200UI();
            m_intDB2LeftSub000200_id = -1;
            LeftSub000200UImode();

        }

        private void butSub000200_19_Click(object sender, EventArgs e)
        {
            initdgvSub000200_01();
            ArrayList AL01 = new ArrayList();
            ArrayList AL02 = new ArrayList();
            ArrayList AL03 = new ArrayList();
            ArrayList AL04 = new ArrayList();
            ArrayList AL05 = new ArrayList();
            AL01.Clear();
            AL02.Clear();
            AL03.Clear();
            AL04.Clear();
            AL05.Clear();

            if (txtSub000200_03.Text != "")
            {
                for (int i = 0; i < dgvSub000200_01.Rows.Count; i++)//取的現行UI上控制器列表所有資料
                {
                    AL01.Add(dgvSub000200_01.Rows[i].Cells[1].Value.ToString());
                    AL02.Add(dgvSub000200_01.Rows[i].Cells[2].Value.ToString());
                    AL03.Add(dgvSub000200_01.Rows[i].Cells[3].Value.ToString());
                    AL04.Add(dgvSub000200_01.Rows[i].Cells[4].Value.ToString());
                    AL05.Add(dgvSub000200_01.Rows[i].Cells[5].Value.ToString());
                }
                try
                {
                    //--
                    //dgvSub000200_01.ReadOnly = true;//唯讀 不可更改
                    dgvSub000200_01.RowHeadersVisible = false;//DataGridView 最前面指示選取列所在位置的箭頭欄位
                    dgvSub000200_01.Rows[0].Selected = false;//取消DataGridView的默認選取(選中)Cell 使其不反藍
                    dgvSub000200_01.AllowUserToAddRows = false;//是否允許使用者新增資料
                    dgvSub000200_01.AllowUserToDeleteRows = false;//是否允許使用者刪除資料
                    dgvSub000200_01.AllowUserToOrderColumns = false;//是否允許使用者調整欄位位置
                    //所有表格欄位寬度全部變成可調 dgvSub000200_01.AllowUserToResizeColumns = false;//是否允許使用者改變欄寬
                    dgvSub000200_01.AllowUserToResizeRows = false;//是否允許使用者改變行高
                    dgvSub000200_01.Columns[1].ReadOnly = true;//單一欄位禁止編輯
                    dgvSub000200_01.Columns[2].ReadOnly = true;//單一欄位禁止編輯
                    dgvSub000200_01.Columns[3].ReadOnly = true;//單一欄位禁止編輯
                    dgvSub000200_01.Columns[4].ReadOnly = true;//單一欄位禁止編輯
                    dgvSub000200_01.Columns[5].ReadOnly = true;//單一欄位禁止編輯
                    dgvSub000200_01.AllowUserToAddRows = false;//刪除空白列
                    dgvSub000200_01.SelectionMode = System.Windows.Forms.DataGridViewSelectionMode.FullRowSelect;//整列選取
                    //--

                    do
                    {
                        for (int i = 0; i < dgvSub000200_01.Rows.Count; i++)
                        {
                            DataGridViewRow r1 = this.dgvSub000200_01.Rows[i];//取得DataGridView整列資料
                            this.dgvSub000200_01.Rows.Remove(r1);//DataGridView刪除整列
                        }
                    } while (dgvSub000200_01.Rows.Count > 0);

                }
                catch
                {
                }
                String StrSearch = txtSub000200_03.Text;
                for (int i = 0; i < AL01.Count; i++)
                {
                    //AL01[i].ToString()->DB index 本來就被隱藏 所以不用在搜尋欄位內
                    if ((AL02[i].ToString().IndexOf(StrSearch) > -1) || (AL03[i].ToString().IndexOf(StrSearch) > -1) || (AL04[i].ToString().IndexOf(StrSearch) > -1) || (AL05[i].ToString().IndexOf(StrSearch) > -1))
                    {
                        this.dgvSub000200_01.Rows.Add(false, AL01[i].ToString(), AL02[i].ToString(), AL03[i].ToString(), AL04[i].ToString(), AL05[i].ToString());
                    }
                }
            }
        }

        private void butSub000200_20_Click(object sender, EventArgs e)//離開
        {
            getTreeView(tvmSub000200_01);//add at 2017/10/06
            m_Sub000200ALData.Clear();//add at 2017/10/06
            m_Sub000200ALData.Add(txtSub000200_01.Text);//add at 2017/10/06
            m_Sub000200ALData.Add(ckbSub000200_01.Checked.ToString());//add at 2017/10/06
            m_Sub000200ALData.Add(ckbSub000200_02.Checked.ToString());//add at 2017/10/06
            m_Sub000200ALData.Add(adpSub000200_01.Value.ToString("yyyy-MM-dd HH:mm"));//add at 2017/10/06
            m_Sub000200ALData.Add(adpSub000200_02.Value.ToString("yyyy-MM-dd HH:mm"));//add at 2017/10/06
            m_Sub000200ALData.Add(rdbSub000200_01.Checked.ToString());//add at 2017/10/06
            m_Sub000200ALData.Add(rdbSub000200_02.Checked.ToString());//add at 2017/10/06
            m_Sub000200ALData.Add(rdbSub000200_03.Checked.ToString());//add at 2017/10/06
            m_Sub000200ALData.Add(rdbSub000200_04.Checked.ToString());//add at 2017/10/06
            for (int i = 0; i < m_ALdoor_group_detail.Count; i++)
            {
                m_Sub000200ALData.Add(m_ALdoor_group_detail[i].ToString());//add at 2017/10/06
            }

            if ((m_intDB2LeftSub000200_id == -1) || CheckUIVarNotChange(m_Sub000200ALInit, m_Sub000200ALData))//if (m_intDB2LeftSub000200_id == -1)
            {
                initSub0002UI();
                Leave_function();
            }
            else
            {
                DialogResult myResult = MessageBox.Show(Language.m_StrControllerMsg00, butSub000200_03.Text.Trim() + "/" + butSub000200_02.Text.Trim(), MessageBoxButtons.YesNo, MessageBoxIcon.Question);
                if (myResult == DialogResult.Yes)
                {
                    initSub0002UI();
                    Leave_function();
                }
            }
        }

        private void ckbSub000200_01_CheckedChanged(object sender, EventArgs e)//啟用群組參數 at 2017/09/20
        {
            if (ckbSub000200_01.Checked==true)
            {
                adpSub000200_01.Enabled = true;
                adpSub000200_02.Enabled = true;
                rdbSub000200_01.Enabled = true;
                rdbSub000200_02.Enabled = true;
                rdbSub000200_03.Enabled = true;
                rdbSub000200_04.Enabled = true;

                rdbSub000200_02.Checked = true;
            }
            else
            {
                adpSub000200_01.Enabled = false;
                adpSub000200_02.Enabled = false;
                rdbSub000200_01.Enabled = false;
                rdbSub000200_02.Enabled = false;
                rdbSub000200_03.Enabled = false;
                rdbSub000200_04.Enabled = false;

                rdbSub000200_01.Checked = false;
                rdbSub000200_02.Checked = false;
                rdbSub000200_03.Checked = false;
                rdbSub000200_04.Checked = false;
            }
        }

        //Sub000200_end
        //Sub0104_start
        private void butSub0104_01_Click(object sender, EventArgs e)//人員車輛部門群組列表-編輯
        {
            try
            {
                int index = dgvSub0104_01.SelectedRows[0].Index;//取得被選取的第一列位置
                String Strid = dgvSub0104_01.Rows[index].Cells[1].Value.ToString();
                m_intdgvSub0104_01_id = Int32.Parse(Strid);

                m_TPOld = m_tabMain.SelectedTab;//--2017/02/22 製作返回按鈕功能
                TabPage_Push();//m_StackTPOld.Push(m_TPOld);//--2017/02/22 製作返回按鈕功能

                m_intDB2LeftSub010400_id = -10;
                initSub010400UI();
                m_tabSub010400.Parent = m_tabMain;

                //--
                //同步子頁的選擇列表 at 2017/07/11
                for (int i = 0; i < dgvSub010400_01.Rows.Count; i++)
                {
                    int id = Convert.ToInt32(dgvSub010400_01.Rows[i].Cells[1].Value.ToString());
                    if (id != m_intdgvSub0104_01_id)
                    {
                        dgvSub010400_01.Rows[i].Selected = false;
                    }
                    else
                    {
                        dgvSub010400_01.Rows[i].Selected = true;
                    }
                }
                //--
                DB2LeftSub010400UI(m_intdgvSub0104_01_id);
                m_tabMain.SelectedTab = m_tabSub010400;

                //--
                //add at 2017/10/12
                m_Sub010400ALInit.Clear();
                m_Sub010400ALInit.Add(txtSub010400_01.Text);
                m_Sub010400ALInit.Add(cmbSub010400_02.SelectedIndex + "");
                m_Sub010400ALInit.Add(adpSub010400_01.Value.ToString("yyyy-MM-dd HH:mm"));
                m_Sub010400ALInit.Add(adpSub010400_02.Value.ToString("yyyy-MM-dd HH:mm"));
                m_Sub010400ALInit.Add(steSub010400_01.StrValue1 + steSub010400_01.StrValue2);
                m_Sub010400ALInit.Add(steSub010400_02.StrValue1 + steSub010400_02.StrValue2);
                m_Sub010400ALInit.Add(steSub010400_03.StrValue1 + steSub010400_03.StrValue2);
                m_Sub010400ALInit.Add(ckbSub010400_21.Checked.ToString());
                m_Sub010400ALInit.Add(ckbSub010400_01.Checked.ToString());
                m_Sub010400ALInit.Add(ckbSub010400_02.Checked.ToString());
                m_Sub010400ALInit.Add(ckbSub010400_12.Checked.ToString());
                m_Sub010400ALInit.Add(ckbSub010400_08.Checked.ToString());
                m_Sub010400ALInit.Add(ckbSub010400_07.Checked.ToString());
                m_Sub010400ALInit.Add(ckbSub010400_09.Checked.ToString());
                m_Sub010400ALInit.Add(ckbSub010400_10.Checked.ToString());
                m_Sub010400ALInit.Add(ckbSub010400_11.Checked.ToString());
                m_Sub010400ALInit.Add(ckbSub010400_13.Checked.ToString());
                m_Sub010400ALInit.Add(ckbSub010400_14.Checked.ToString());
                m_Sub010400ALInit.Add(ckbSub010400_15.Checked.ToString());
                m_Sub010400ALInit.Add(ckbSub010400_16.Checked.ToString());
                m_Sub010400ALInit.Add(ckbSub010400_17.Checked.ToString());
                m_Sub010400ALInit.Add(ckbSub010400_18.Checked.ToString());
                m_Sub010400ALInit.Add(ckbSub010400_19.Checked.ToString());
                m_Sub010400ALInit.Add(ckbSub010400_20.Checked.ToString());
                m_Sub010400ALInit.Add(rdbSub010400_01.Checked.ToString());
                m_Sub010400ALInit.Add(rdbSub010400_02.Checked.ToString());
                m_Sub010400ALInit.Add(rdbSub010400_03.Checked.ToString());
                m_Sub010400ALInit.Add(rdbSub010400_04.Checked.ToString());

                DeptCardTree_NodeFun DeptCardTree_NodeFun1 = new DeptCardTree_NodeFun();
                DeptCardTree_NodeFun1.getTreeView(tvmSub010400_01);

                for (int i = 0; i < DeptCardTree_NodeFun1.m_ALuser_car_group_detailed.Count; i++)
                {
                    m_Sub010400ALInit.Add(DeptCardTree_NodeFun1.m_ALuser_car_group_detailed[i].ToString());
                }
                //--
            }
            catch
            {

            }

        }

        private void butSub0104_02_Click(object sender, EventArgs e)//人員車輛部門群組列表-新增
        {
            m_TPOld = m_tabMain.SelectedTab;//--2017/02/22 製作返回按鈕功能
            TabPage_Push();//m_StackTPOld.Push(m_TPOld);//--2017/02/22 製作返回按鈕功能

            m_intdgvSub0104_01_id = -10;
            m_intDB2LeftSub010400_id = m_intdgvSub0104_01_id;
            initSub010400UI();

            m_tabSub010400.Parent = m_tabMain;
            m_tabMain.SelectedTab = m_tabSub010400;

            //---
            //新增所有群組時都預設填入名稱
            txtSub010400_01.Text = "group_usercar_" + DateTime.Now.ToString("yyyyMMddHHmmss");
            //---新增所有群組時都預設填入名稱

            //--
            //add at 2017/10/12
            m_Sub010400ALInit.Clear();
            m_Sub010400ALInit.Add(txtSub010400_01.Text);
            m_Sub010400ALInit.Add(cmbSub010400_02.SelectedIndex + "");
            m_Sub010400ALInit.Add(adpSub010400_01.Value.ToString("yyyy-MM-dd HH:mm"));
            m_Sub010400ALInit.Add(adpSub010400_02.Value.ToString("yyyy-MM-dd HH:mm"));
            m_Sub010400ALInit.Add(steSub010400_01.StrValue1 + steSub010400_01.StrValue2);
            m_Sub010400ALInit.Add(steSub010400_02.StrValue1 + steSub010400_02.StrValue2);
            m_Sub010400ALInit.Add(steSub010400_03.StrValue1 + steSub010400_03.StrValue2);
            m_Sub010400ALInit.Add(ckbSub010400_21.Checked.ToString());
            m_Sub010400ALInit.Add(ckbSub010400_01.Checked.ToString());
            m_Sub010400ALInit.Add(ckbSub010400_02.Checked.ToString());
            m_Sub010400ALInit.Add(ckbSub010400_12.Checked.ToString());
            m_Sub010400ALInit.Add(ckbSub010400_08.Checked.ToString());
            m_Sub010400ALInit.Add(ckbSub010400_07.Checked.ToString());
            m_Sub010400ALInit.Add(ckbSub010400_09.Checked.ToString());
            m_Sub010400ALInit.Add(ckbSub010400_10.Checked.ToString());
            m_Sub010400ALInit.Add(ckbSub010400_11.Checked.ToString());
            m_Sub010400ALInit.Add(ckbSub010400_13.Checked.ToString());
            m_Sub010400ALInit.Add(ckbSub010400_14.Checked.ToString());
            m_Sub010400ALInit.Add(ckbSub010400_15.Checked.ToString());
            m_Sub010400ALInit.Add(ckbSub010400_16.Checked.ToString());
            m_Sub010400ALInit.Add(ckbSub010400_17.Checked.ToString());
            m_Sub010400ALInit.Add(ckbSub010400_18.Checked.ToString());
            m_Sub010400ALInit.Add(ckbSub010400_19.Checked.ToString());
            m_Sub010400ALInit.Add(ckbSub010400_20.Checked.ToString());
            m_Sub010400ALInit.Add(rdbSub010400_01.Checked.ToString());
            m_Sub010400ALInit.Add(rdbSub010400_02.Checked.ToString());
            m_Sub010400ALInit.Add(rdbSub010400_03.Checked.ToString());
            m_Sub010400ALInit.Add(rdbSub010400_04.Checked.ToString());
            //--
        }

        private void butSub0104_06_Click(object sender, EventArgs e)//人員車輛部門群組列表-全選
        {
            /*
            for (int i = 0; i < dgvSub0104_01.Rows.Count; i++)
            {
                dgvSub0104_01.Rows[i].Cells[0].Value = true;
                dgvSub0104_01.Rows[i].Selected = true;
            }
            */
            dgvSub0104_01.SelectAll();//把所有列表元件的全選/取消全選改成SelectAll()/ClearSelection()
        }

        private void butSub0104_07_Click(object sender, EventArgs e)//人員車輛部門群組列表-取消全選
        {
            /*
            for (int i = 0; i < dgvSub0104_01.Rows.Count; i++)
            {
                dgvSub0104_01.Rows[i].Cells[0].Value = false;
                dgvSub0104_01.Rows[i].Selected = false;
            }
            */
            dgvSub0104_01.ClearSelection();//把所有列表元件的全選/取消全選改成SelectAll()/ClearSelection()
        }

        private void dgvSub0104_01_DoubleClick(object sender, EventArgs e)//人員車輛部門群組列表-雙點擊編輯
        {
            butSub0104_01.PerformClick();
        }

        public int m_intdgvSub0104_01_id = -1;
        private void dgvSub0104_01_SelectionChanged(object sender, EventArgs e)//人員車輛部門群組列表-改變選擇編輯ID
        {
            //---
            //列表元件shift全選時最前面的狀態要能自動打勾，反之要能取消
            for (int i = 0; i < dgvSub0104_01.Rows.Count; i++)
            {
                dgvSub0104_01.Rows[i].Cells[0].Value = false;
            }
            for (int j = 0; j < dgvSub0104_01.SelectedRows.Count; j++)
            {
                dgvSub0104_01.SelectedRows[j].Cells[0].Value = true;
            }
            //---列表元件shift全選時最前面的狀態要能自動打勾，反之要能取消

            try
            {
                int index = dgvSub0104_01.SelectedRows[0].Index;//取得被選取的第一列位置
                String Strid = dgvSub0104_01.Rows[index].Cells[1].Value.ToString();
                m_intdgvSub0104_01_id = Int32.Parse(Strid);
            }
            catch
            {
            }
        }

        private void butSub0104_08_Click(object sender, EventArgs e)//人員車輛部門群組列表-批次處理
        {
            ArrayList ALSN = new ArrayList();
            ALSN.Clear();
            for (int i = 0; i < dgvSub0104_01.Rows.Count; i++)
            {
                String data = dgvSub0104_01.Rows[i].Cells[0].Value.ToString().ToLower();//抓取DataGridView欄位資料
                if (data == "true")
                {
                    ALSN.Add(dgvSub0104_01.Rows[i].Cells[1].Value.ToString());//抓 ID
                }
            }
            String SQL = "";
            switch (cmbSub0104_01.SelectedIndex)
            {
                case 0:
                    SQL = "";
                    for (int i = 0; i < ALSN.Count; i++)
                    {
                        SQL += "UPDATE user_car_group SET enable = 1,state=1 WHERE (id = " + ALSN[i].ToString() + ");";
                    }

                    break;
                case 1:
                    SQL = "";
                    for (int i = 0; i < ALSN.Count; i++)
                    {
                        SQL += "UPDATE user_car_group SET enable = 0,state=1 WHERE (id = " + ALSN[i].ToString() + ");";
                    }
                    break;
                case 2:
                    SQL = "";
                    for (int i = 0; i < ALSN.Count; i++)
                    {
                        SQL += String.Format("DELETE FROM user_car_group WHERE id={0};DELETE FROM user_car_group_detailed WHERE user_car_group_id={0};", ALSN[i].ToString());
                    }
                    break;
            }
            MySQL.InsertUpdateDelete(SQL);//新增資料程式

            initdgvSub0104_01();
        }

        private void butSub0104_09_Click(object sender, EventArgs e)//人員車輛部門群組列表-搜尋
        {
            initdgvSub0104_01();
            ArrayList AL01 = new ArrayList();
            ArrayList AL02 = new ArrayList();
            ArrayList AL03 = new ArrayList();
            ArrayList AL04 = new ArrayList();
            ArrayList AL05 = new ArrayList();
            AL01.Clear();
            AL02.Clear();
            AL03.Clear();
            AL04.Clear();
            AL05.Clear();

            if (txtSub0104_01.Text != "")
            {
                for (int i = 0; i < dgvSub0104_01.Rows.Count; i++)//取的現行UI上控制器列表所有資料
                {
                    AL01.Add(dgvSub0104_01.Rows[i].Cells[1].Value.ToString());
                    AL02.Add(dgvSub0104_01.Rows[i].Cells[2].Value.ToString());
                    AL03.Add(dgvSub0104_01.Rows[i].Cells[3].Value.ToString());
                    AL04.Add(dgvSub0104_01.Rows[i].Cells[4].Value.ToString());
                    AL05.Add(dgvSub0104_01.Rows[i].Cells[5].Value.ToString());
                }
                try
                {
                    //--
                    //dgvSub0104_01.ReadOnly = true;//唯讀 不可更改
                    dgvSub0104_01.RowHeadersVisible = false;//DataGridView 最前面指示選取列所在位置的箭頭欄位
                    dgvSub0104_01.Rows[0].Selected = false;//取消DataGridView的默認選取(選中)Cell 使其不反藍
                    dgvSub0104_01.AllowUserToAddRows = false;//是否允許使用者新增資料
                    dgvSub0104_01.AllowUserToDeleteRows = false;//是否允許使用者刪除資料
                    dgvSub0104_01.AllowUserToOrderColumns = false;//是否允許使用者調整欄位位置
                    //所有表格欄位寬度全部變成可調 dgvSub0104_01.AllowUserToResizeColumns = false;//是否允許使用者改變欄寬
                    dgvSub0104_01.AllowUserToResizeRows = false;//是否允許使用者改變行高
                    dgvSub0104_01.Columns[1].ReadOnly = true;//單一欄位禁止編輯
                    dgvSub0104_01.Columns[2].ReadOnly = true;//單一欄位禁止編輯
                    dgvSub0104_01.Columns[3].ReadOnly = true;//單一欄位禁止編輯
                    dgvSub0104_01.Columns[4].ReadOnly = true;//單一欄位禁止編輯
                    dgvSub0104_01.Columns[5].ReadOnly = true;//單一欄位禁止編輯
                    dgvSub0104_01.AllowUserToAddRows = false;//刪除空白列
                    dgvSub0104_01.SelectionMode = System.Windows.Forms.DataGridViewSelectionMode.FullRowSelect;//整列選取
                    //--

                    do
                    {
                        for (int i = 0; i < dgvSub0104_01.Rows.Count; i++)
                        {
                            DataGridViewRow r1 = this.dgvSub0104_01.Rows[i];//取得DataGridView整列資料
                            this.dgvSub0104_01.Rows.Remove(r1);//DataGridView刪除整列
                        }
                    } while (dgvSub0104_01.Rows.Count > 0);

                }
                catch
                {
                }
                String StrSearch = txtSub0104_01.Text;
                for (int i = 0; i < AL01.Count; i++)
                {
                    //AL01[i].ToString()->DB index 本來就被隱藏 所以不用在搜尋欄位內
                    if ((AL02[i].ToString().IndexOf(StrSearch) > -1) || (AL03[i].ToString().IndexOf(StrSearch) > -1) || (AL04[i].ToString().IndexOf(StrSearch) > -1) || (AL05[i].ToString().IndexOf(StrSearch) > -1))
                    {
                        this.dgvSub0104_01.Rows.Add(false, AL01[i].ToString(), AL02[i].ToString(), AL03[i].ToString(), AL04[i].ToString(), AL05[i].ToString());
                    }
                }
            }
        }

        private void ckbSub0104_01_CheckedChanged(object sender, EventArgs e)
        {
            m_StrdgvSub0104_01_ext01 = "";
            if (ckbSub0104_01.Checked)
            {
                if (ckbSub0104_02.Checked)//(1,1)
                {
                    m_StrdgvSub0104_01_ext01 = "";//兩個都選等於沒選
                }
                else//(1,0)
                {
                    m_StrdgvSub0104_01_ext01 = " WHERE enable = 1";
                }
            }
            else
            {
                if (ckbSub0104_02.Checked)//(0,1)
                {
                    m_StrdgvSub0104_01_ext01 = " WHERE enable = 0";
                }
                else//(0,0)
                {
                    m_StrdgvSub0104_01_ext01 = "";//沒選
                }
            }
            initdgvSub0104_01();
        }
        //Sub0104_end
        //Sub010400_start
        private void ckbSub010400_01_CheckedChanged(object sender, EventArgs e)//設置卡片類型
        {
            if (ckbSub010400_01.Checked == true)
            {
                cmbSub010400_02.Enabled = true;
                //--
                //修正人員車輛群組在啟動卡片參數時賦予預設值
                if (cmbSub010400_02.SelectedIndex == -1)
                {
                    cmbSub010400_02.SelectedIndex = 0;
                }
                //--
            }
            else
            {
                cmbSub010400_02.Enabled = false;

                cmbSub010400_02.SelectedIndex = -1;//修正人員車輛群組在啟動卡片參數時賦予預設值
            }
        }

        private void ckbSub010400_02_CheckedChanged(object sender, EventArgs e)//設置有效期
        {
            if (ckbSub010400_02.Checked == true)
            {
                adpSub010400_01.Enabled = true;
                adpSub010400_02.Enabled = true;
            }
            else
            {
                adpSub010400_01.Enabled = false;
                adpSub010400_02.Enabled = false;
            }
        }

        private void ckbSub010400_08_CheckedChanged(object sender, EventArgs e)//設置卡片狀態
        {
            if (ckbSub010400_08.Checked == true)
            {
                ckbSub010400_09.Enabled = true;
                ckbSub010400_10.Enabled = true;
                ckbSub010400_11.Enabled = true;
            }
            else
            {
                ckbSub010400_09.Enabled = false;
                ckbSub010400_10.Enabled = false;
                ckbSub010400_11.Enabled = false;
            }
        }

        private void ckbSub010400_07_CheckedChanged(object sender, EventArgs e)//設置通行等級
        {
            if (ckbSub010400_07.Checked == true)
            {
                rdbSub010400_02.Checked = true;

                rdbSub010400_01.Enabled = true;
                rdbSub010400_02.Enabled = true;
                rdbSub010400_03.Enabled = true;
                rdbSub010400_04.Enabled = true;
            }
            else
            {
                rdbSub010400_01.Checked = false;
                rdbSub010400_02.Checked = false;
                rdbSub010400_03.Checked = false;
                rdbSub010400_04.Checked = false;

                rdbSub010400_01.Enabled = false;
                rdbSub010400_02.Enabled = false;
                rdbSub010400_03.Enabled = false;
                rdbSub010400_04.Enabled = false;
            }
        }

        private void ckbSub010400_12_CheckedChanged(object sender, EventArgs e)//設置週計畫與通行時段
        {
            if (ckbSub010400_12.Checked == true)
            {
                ckbSub010400_13.Enabled = true;
                ckbSub010400_14.Enabled = true;
                ckbSub010400_15.Enabled = true;
                ckbSub010400_16.Enabled = true;
                ckbSub010400_17.Enabled = true;
                ckbSub010400_18.Enabled = true;
                ckbSub010400_19.Enabled = true;
                ckbSub010400_20.Enabled = true;
                steSub010400_01.Enabled = true;
                steSub010400_02.Enabled = true;
                steSub010400_03.Enabled = true;
            }
            else
            {
                ckbSub010400_13.Enabled = false;
                ckbSub010400_14.Enabled = false;
                ckbSub010400_15.Enabled = false;
                ckbSub010400_16.Enabled = false;
                ckbSub010400_17.Enabled = false;
                ckbSub010400_18.Enabled = false;
                ckbSub010400_19.Enabled = false;
                ckbSub010400_20.Enabled = false;
                steSub010400_01.Enabled = false;
                steSub010400_02.Enabled = false;
                steSub010400_03.Enabled = false;
            }
        }

        private void ckbSub010400_21_CheckedChanged(object sender, EventArgs e)//群組參數啟用
        {
            if (ckbSub010400_21.Checked == true)
            {
                ckbSub010400_01.Enabled = true;
                ckbSub010400_08.Enabled = true;
                ckbSub010400_07.Enabled = true;
                ckbSub010400_02.Enabled = true;
                ckbSub010400_12.Enabled = true;
            }
            else
            {
                ckbSub010400_01.Enabled = false;
                ckbSub010400_08.Enabled = false;
                ckbSub010400_07.Enabled = false;
                ckbSub010400_02.Enabled = false;
                ckbSub010400_12.Enabled = false;

                //--
                //add at 2017/10/17
                ckbSub010400_01.Checked = false;
                ckbSub010400_08.Checked = false;
                ckbSub010400_07.Checked = false;
                ckbSub010400_02.Checked = false;
                ckbSub010400_12.Checked = false;
                //--
            }
        }

        private void dgvSub010400_01_DoubleClick(object sender, EventArgs e)
        {
            butSub010400_11.PerformClick();
        }

        private void butSub010400_11_Click(object sender, EventArgs e)//編輯
        {
            int intbuf =- 1;
            try
            {
                int index = dgvSub010400_01.SelectedRows[0].Index;//取得被選取的第一列位置
                String Strid = dgvSub010400_01.Rows[index].Cells[1].Value.ToString();
                intbuf = Int32.Parse(Strid);

            }
            catch
            {
            }
            if(intbuf > 0)
            {
                m_intDB2LeftSub010400_id = -10;
                initSub010400UI();
                //--
                //同步子頁的選擇列表 at 2017/07/11
                for (int i = 0; i < dgvSub010400_01.Rows.Count; i++)
                {
                    int id = Convert.ToInt32(dgvSub010400_01.Rows[i].Cells[1].Value.ToString());
                    if (id != intbuf)
                    {
                        dgvSub010400_01.Rows[i].Selected = false;
                    }
                    else
                    {
                        dgvSub010400_01.Rows[i].Selected = true;
                    }
                }
                //--
                m_intDB2LeftSub010400_id = intbuf;
                DB2LeftSub010400UI(m_intDB2LeftSub010400_id);

                //--
                //add at 2017/10/12
                m_Sub010400ALInit.Clear();
                m_Sub010400ALInit.Add(txtSub010400_01.Text);
                m_Sub010400ALInit.Add(cmbSub010400_02.SelectedIndex + "");
                m_Sub010400ALInit.Add(adpSub010400_01.Value.ToString("yyyy-MM-dd HH:mm"));
                m_Sub010400ALInit.Add(adpSub010400_02.Value.ToString("yyyy-MM-dd HH:mm"));
                m_Sub010400ALInit.Add(steSub010400_01.StrValue1 + steSub010400_01.StrValue2);
                m_Sub010400ALInit.Add(steSub010400_02.StrValue1 + steSub010400_02.StrValue2);
                m_Sub010400ALInit.Add(steSub010400_03.StrValue1 + steSub010400_03.StrValue2);
                m_Sub010400ALInit.Add(ckbSub010400_21.Checked.ToString());
                m_Sub010400ALInit.Add(ckbSub010400_01.Checked.ToString());
                m_Sub010400ALInit.Add(ckbSub010400_02.Checked.ToString());
                m_Sub010400ALInit.Add(ckbSub010400_12.Checked.ToString());
                m_Sub010400ALInit.Add(ckbSub010400_08.Checked.ToString());
                m_Sub010400ALInit.Add(ckbSub010400_07.Checked.ToString());
                m_Sub010400ALInit.Add(ckbSub010400_09.Checked.ToString());
                m_Sub010400ALInit.Add(ckbSub010400_10.Checked.ToString());
                m_Sub010400ALInit.Add(ckbSub010400_11.Checked.ToString());
                m_Sub010400ALInit.Add(ckbSub010400_13.Checked.ToString());
                m_Sub010400ALInit.Add(ckbSub010400_14.Checked.ToString());
                m_Sub010400ALInit.Add(ckbSub010400_15.Checked.ToString());
                m_Sub010400ALInit.Add(ckbSub010400_16.Checked.ToString());
                m_Sub010400ALInit.Add(ckbSub010400_17.Checked.ToString());
                m_Sub010400ALInit.Add(ckbSub010400_18.Checked.ToString());
                m_Sub010400ALInit.Add(ckbSub010400_19.Checked.ToString());
                m_Sub010400ALInit.Add(ckbSub010400_20.Checked.ToString());
                m_Sub010400ALInit.Add(rdbSub010400_01.Checked.ToString());
                m_Sub010400ALInit.Add(rdbSub010400_02.Checked.ToString());
                m_Sub010400ALInit.Add(rdbSub010400_03.Checked.ToString());
                m_Sub010400ALInit.Add(rdbSub010400_04.Checked.ToString());

                DeptCardTree_NodeFun DeptCardTree_NodeFun1 = new DeptCardTree_NodeFun();
                DeptCardTree_NodeFun1.getTreeView(tvmSub010400_01);

                for (int i = 0; i < DeptCardTree_NodeFun1.m_ALuser_car_group_detailed.Count; i++)
                {
                    m_Sub010400ALInit.Add(DeptCardTree_NodeFun1.m_ALuser_car_group_detailed[i].ToString());
                }
                //--
            }
        }

        private void butSub010400_16_Click(object sender, EventArgs e)//全選
        {
            /*
            for (int i = 0; i < dgvSub010400_01.Rows.Count; i++)
            {
                dgvSub010400_01.Rows[i].Cells[0].Value = true;
                dgvSub010400_01.Rows[i].Selected = true;
            }
            */
            dgvSub010400_01.SelectAll();//把所有列表元件的全選/取消全選改成SelectAll()/ClearSelection()
        }

        private void butSub010400_17_Click(object sender, EventArgs e)//取消全選
        {
            /*
            for (int i = 0; i < dgvSub010400_01.Rows.Count; i++)
            {
                dgvSub010400_01.Rows[i].Cells[0].Value = false;
                dgvSub010400_01.Rows[i].Selected = false;
            }
            */
            dgvSub010400_01.ClearSelection();//把所有列表元件的全選/取消全選改成SelectAll()/ClearSelection()
        }

        private void butSub010400_12_Click(object sender, EventArgs e)//新增
        {
            m_intDB2LeftSub010400_id = -10;
            DB2LeftSub010400UI(m_intDB2LeftSub010400_id);

            //--
            //add at 2017/10/12
            m_Sub010400ALInit.Clear();
            m_Sub010400ALInit.Add(txtSub010400_01.Text);
            m_Sub010400ALInit.Add(cmbSub010400_02.SelectedIndex + "");
            m_Sub010400ALInit.Add(adpSub010400_01.Value.ToString("yyyy-MM-dd HH:mm"));
            m_Sub010400ALInit.Add(adpSub010400_02.Value.ToString("yyyy-MM-dd HH:mm"));
            m_Sub010400ALInit.Add(steSub010400_01.StrValue1 + steSub010400_01.StrValue2);
            m_Sub010400ALInit.Add(steSub010400_02.StrValue1 + steSub010400_02.StrValue2);
            m_Sub010400ALInit.Add(steSub010400_03.StrValue1 + steSub010400_03.StrValue2);
            m_Sub010400ALInit.Add(ckbSub010400_21.Checked.ToString());
            m_Sub010400ALInit.Add(ckbSub010400_01.Checked.ToString());
            m_Sub010400ALInit.Add(ckbSub010400_02.Checked.ToString());
            m_Sub010400ALInit.Add(ckbSub010400_12.Checked.ToString());
            m_Sub010400ALInit.Add(ckbSub010400_08.Checked.ToString());
            m_Sub010400ALInit.Add(ckbSub010400_07.Checked.ToString());
            m_Sub010400ALInit.Add(ckbSub010400_09.Checked.ToString());
            m_Sub010400ALInit.Add(ckbSub010400_10.Checked.ToString());
            m_Sub010400ALInit.Add(ckbSub010400_11.Checked.ToString());
            m_Sub010400ALInit.Add(ckbSub010400_13.Checked.ToString());
            m_Sub010400ALInit.Add(ckbSub010400_14.Checked.ToString());
            m_Sub010400ALInit.Add(ckbSub010400_15.Checked.ToString());
            m_Sub010400ALInit.Add(ckbSub010400_16.Checked.ToString());
            m_Sub010400ALInit.Add(ckbSub010400_17.Checked.ToString());
            m_Sub010400ALInit.Add(ckbSub010400_18.Checked.ToString());
            m_Sub010400ALInit.Add(ckbSub010400_19.Checked.ToString());
            m_Sub010400ALInit.Add(ckbSub010400_20.Checked.ToString());
            m_Sub010400ALInit.Add(rdbSub010400_01.Checked.ToString());
            m_Sub010400ALInit.Add(rdbSub010400_02.Checked.ToString());
            m_Sub010400ALInit.Add(rdbSub010400_03.Checked.ToString());
            m_Sub010400ALInit.Add(rdbSub010400_04.Checked.ToString());
            //--
        }

        private void butSub010400_18_Click(object sender, EventArgs e)//批次執行
        {
            ArrayList ALSN = new ArrayList();
            ALSN.Clear();
            for (int i = 0; i < dgvSub010400_01.Rows.Count; i++)
            {
                String data = dgvSub010400_01.Rows[i].Cells[0].Value.ToString().ToLower();//抓取DataGridView欄位資料
                if (data == "true")
                {
                    ALSN.Add(dgvSub010400_01.Rows[i].Cells[1].Value.ToString());//抓 ID
                }
            }
            String SQL = "";
            switch (cmbSub010400_01.SelectedIndex)
            {
                case 0:
                    SQL = "";
                    for (int i = 0; i < ALSN.Count; i++)
                    {
                        SQL += "UPDATE user_car_group SET enable = 1,state=1 WHERE (id = " + ALSN[i].ToString() + ");";
                    }

                    break;
                case 1:
                    SQL = "";
                    for (int i = 0; i < ALSN.Count; i++)
                    {
                        SQL += "UPDATE user_car_group SET enable = 0,state=1 WHERE (id = " + ALSN[i].ToString() + ");";
                    }
                    break;
                case 2:
                    SQL = "";
                    for (int i = 0; i < ALSN.Count; i++)
                    {
                        SQL += String.Format("DELETE FROM user_car_group WHERE id={0};DELETE FROM user_car_group_detailed WHERE user_car_group_id={0};", ALSN[i].ToString());
                    }
                    break;
            }
            MySQL.InsertUpdateDelete(SQL);//新增資料程式

            initdgvSub010400_01();

            initLeftSub010400UI();
            m_intDB2LeftSub010400_id = -1;
            LeftSub010400UImode();
        }

        private void butSub010400_19_Click(object sender, EventArgs e)//搜尋
        {
            initdgvSub010400_01();
            ArrayList AL01 = new ArrayList();
            ArrayList AL02 = new ArrayList();
            ArrayList AL03 = new ArrayList();
            ArrayList AL04 = new ArrayList();
            ArrayList AL05 = new ArrayList();
            AL01.Clear();
            AL02.Clear();
            AL03.Clear();
            AL04.Clear();
            AL05.Clear();

            if (txtSub010400_03.Text != "")
            {
                for (int i = 0; i < dgvSub010400_01.Rows.Count; i++)//取的現行UI上控制器列表所有資料
                {
                    AL01.Add(dgvSub010400_01.Rows[i].Cells[1].Value.ToString());
                    AL02.Add(dgvSub010400_01.Rows[i].Cells[2].Value.ToString());
                    AL03.Add(dgvSub010400_01.Rows[i].Cells[3].Value.ToString());
                    AL04.Add(dgvSub010400_01.Rows[i].Cells[4].Value.ToString());
                    AL05.Add(dgvSub010400_01.Rows[i].Cells[5].Value.ToString());
                }
                try
                {
                    //--
                    //dgvSub010400_01.ReadOnly = true;//唯讀 不可更改
                    dgvSub010400_01.RowHeadersVisible = false;//DataGridView 最前面指示選取列所在位置的箭頭欄位
                    dgvSub010400_01.Rows[0].Selected = false;//取消DataGridView的默認選取(選中)Cell 使其不反藍
                    dgvSub010400_01.AllowUserToAddRows = false;//是否允許使用者新增資料
                    dgvSub010400_01.AllowUserToDeleteRows = false;//是否允許使用者刪除資料
                    dgvSub010400_01.AllowUserToOrderColumns = false;//是否允許使用者調整欄位位置
                    //所有表格欄位寬度全部變成可調 dgvSub010400_01.AllowUserToResizeColumns = false;//是否允許使用者改變欄寬
                    dgvSub010400_01.AllowUserToResizeRows = false;//是否允許使用者改變行高
                    dgvSub010400_01.Columns[1].ReadOnly = true;//單一欄位禁止編輯
                    dgvSub010400_01.Columns[2].ReadOnly = true;//單一欄位禁止編輯
                    dgvSub010400_01.Columns[3].ReadOnly = true;//單一欄位禁止編輯
                    dgvSub010400_01.Columns[4].ReadOnly = true;//單一欄位禁止編輯
                    dgvSub010400_01.Columns[5].ReadOnly = true;//單一欄位禁止編輯
                    dgvSub010400_01.AllowUserToAddRows = false;//刪除空白列
                    dgvSub010400_01.SelectionMode = System.Windows.Forms.DataGridViewSelectionMode.FullRowSelect;//整列選取
                    //--

                    do
                    {
                        for (int i = 0; i < dgvSub010400_01.Rows.Count; i++)
                        {
                            DataGridViewRow r1 = this.dgvSub010400_01.Rows[i];//取得DataGridView整列資料
                            this.dgvSub010400_01.Rows.Remove(r1);//DataGridView刪除整列
                        }
                    } while (dgvSub010400_01.Rows.Count > 0);

                }
                catch
                {
                }
                String StrSearch = txtSub010400_03.Text;
                for (int i = 0; i < AL01.Count; i++)
                {
                    //AL01[i].ToString()->DB index 本來就被隱藏 所以不用在搜尋欄位內
                    if ((AL02[i].ToString().IndexOf(StrSearch) > -1) || (AL03[i].ToString().IndexOf(StrSearch) > -1) || (AL04[i].ToString().IndexOf(StrSearch) > -1) || (AL05[i].ToString().IndexOf(StrSearch) > -1))
                    {
                        this.dgvSub010400_01.Rows.Add(false, AL01[i].ToString(), AL02[i].ToString(), AL03[i].ToString(), AL04[i].ToString(), AL05[i].ToString());
                    }
                }
            }
        }

        private void butSub010400_06_Click(object sender, EventArgs e)//TreeView 全選
        {
            for (int i = 0; i < tvmSub010400_01.Nodes.Count; i++)
            {
                Tree_Node tmp = ((Tree_Node)tvmSub010400_01.Nodes[i]);
                tmp.Checked = true;
                DeptCardTree_NodeFun.SetChildNodeCheckedState(tmp, tmp.Checked);
            }
        }

        private void butSub010400_07_Click(object sender, EventArgs e)//TreeView 取消全選
        {
            for (int i = 0; i < tvmSub010400_01.Nodes.Count; i++)
            {
                Tree_Node tmp = ((Tree_Node)tvmSub010400_01.Nodes[i]);
                tmp.Checked = false;
                DeptCardTree_NodeFun.SetChildNodeCheckedState(tmp, tmp.Checked);
            }
        }

        public void DeptCardGroupUI2DB(bool blnRunSQL=true,int state=1)
        {
            String Strname, Strenable, Strcard_type_enable, Strcard_type, Streffective_date_enable, Streffective_date_start, Streffective_date_end, Strcard_status_enable, Strcard_disable, Strcard_black, Strcard_apb_disable, Strcard_level_enable, Strcard_level, Strcard_week_time_enable, Strcard_week, Strcard_time_period01, Strcard_time_period02, Strcard_time_period03;
            String SQL = "";

            Strname = txtSub010400_01.Text;
            Strenable = "0";
            Strcard_type_enable = "0";
            Strcard_type = "-10";
            Streffective_date_enable = "0";
            Strcard_status_enable = "0";
            Strcard_disable = "0";
            Strcard_black = "0";
            Strcard_apb_disable = "0";
            Strcard_level_enable = "0";
            Strcard_level = "-1";
            Strcard_week_time_enable = "0";
            Strcard_week = "0";

            if (ckbSub010400_21.Checked == true)
            {
                Strenable = "1";
            }

            if (ckbSub010400_01.Checked == true)
            {
                Strcard_type_enable = "1";
            }

            if (cmbSub010400_02.SelectedIndex >= 0)
            {
                Strcard_type = m_ALCardType_ID[cmbSub010400_02.SelectedIndex].ToString();
            }

            if (ckbSub010400_02.Checked == true)
            {
                Streffective_date_enable = "1";
            }

            Streffective_date_start = adpSub010400_01.Value.ToString("yyyy/MM/dd HH:mm");
            Streffective_date_end = adpSub010400_02.Value.ToString("yyyy/MM/dd HH:mm");

            if (ckbSub010400_08.Checked == true)
            {
                Strcard_status_enable = "1";
            }

            if (ckbSub010400_09.Checked == true)
            {
                Strcard_disable = "1";
            }
            if(ckbSub010400_10.Checked == true)
            {
                Strcard_black = "1";
            }

            if (ckbSub010400_11.Checked == true)
            {
                Strcard_apb_disable = "1";
            }

            if (ckbSub010400_07.Checked == true)
            {
                Strcard_level_enable = "1";
            }

            if(rdbSub010400_01.Checked == true)
            {
                Strcard_level = "0";
            }
            else if(rdbSub010400_02.Checked == true)
            {
                Strcard_level = "1";
            }
            else if(rdbSub010400_03.Checked == true)
            {
                Strcard_level = "2";
            }
            else if (rdbSub010400_04.Checked == true)
            {
                Strcard_level = "3";
            }

            if (ckbSub010400_12.Checked == true)
            {
                Strcard_week_time_enable = "1";
            }

            int v1 = 0;
            v1 += Convert.ToInt32(ckbSub010400_13.Checked) * 1;
            v1 += Convert.ToInt32(ckbSub010400_14.Checked) * 2;
            v1 += Convert.ToInt32(ckbSub010400_15.Checked) * 4;
            v1 += Convert.ToInt32(ckbSub010400_16.Checked) * 8;
            v1 += Convert.ToInt32(ckbSub010400_17.Checked) * 16;
            v1 += Convert.ToInt32(ckbSub010400_18.Checked) * 32;
            v1 += Convert.ToInt32(ckbSub010400_19.Checked) * 64;
            v1 += Convert.ToInt32(ckbSub010400_20.Checked) * 128;
            Strcard_week = v1+"";

            Strcard_time_period01 = steSub010400_01.StrValue1 + "~" + steSub010400_01.StrValue2;

            Strcard_time_period02 = steSub010400_02.StrValue1 + "~" + steSub010400_02.StrValue2;

            Strcard_time_period03 = steSub010400_03.StrValue1 + "~" + steSub010400_03.StrValue2;

            DeptCardTree_NodeFun DeptCardTree_NodeFun1 = new DeptCardTree_NodeFun();
            DeptCardTree_NodeFun1.getTreeView(tvmSub010400_01);

            //--
            //add at 2017/10/12
            if (!blnRunSQL)
            {
                m_Sub010400ALData.Clear();
                m_Sub010400ALData.Add(txtSub010400_01.Text);
                m_Sub010400ALData.Add(cmbSub010400_02.SelectedIndex + "");
                m_Sub010400ALData.Add(adpSub010400_01.Value.ToString("yyyy-MM-dd HH:mm"));
                m_Sub010400ALData.Add(adpSub010400_02.Value.ToString("yyyy-MM-dd HH:mm"));
                m_Sub010400ALData.Add(steSub010400_01.StrValue1 + steSub010400_01.StrValue2);
                m_Sub010400ALData.Add(steSub010400_02.StrValue1 + steSub010400_02.StrValue2);
                m_Sub010400ALData.Add(steSub010400_03.StrValue1 + steSub010400_03.StrValue2);
                m_Sub010400ALData.Add(ckbSub010400_21.Checked.ToString());
                m_Sub010400ALData.Add(ckbSub010400_01.Checked.ToString());
                m_Sub010400ALData.Add(ckbSub010400_02.Checked.ToString());
                m_Sub010400ALData.Add(ckbSub010400_12.Checked.ToString());
                m_Sub010400ALData.Add(ckbSub010400_08.Checked.ToString());
                m_Sub010400ALData.Add(ckbSub010400_07.Checked.ToString());
                m_Sub010400ALData.Add(ckbSub010400_09.Checked.ToString());
                m_Sub010400ALData.Add(ckbSub010400_10.Checked.ToString());
                m_Sub010400ALData.Add(ckbSub010400_11.Checked.ToString());
                m_Sub010400ALData.Add(ckbSub010400_13.Checked.ToString());
                m_Sub010400ALData.Add(ckbSub010400_14.Checked.ToString());
                m_Sub010400ALData.Add(ckbSub010400_15.Checked.ToString());
                m_Sub010400ALData.Add(ckbSub010400_16.Checked.ToString());
                m_Sub010400ALData.Add(ckbSub010400_17.Checked.ToString());
                m_Sub010400ALData.Add(ckbSub010400_18.Checked.ToString());
                m_Sub010400ALData.Add(ckbSub010400_19.Checked.ToString());
                m_Sub010400ALData.Add(ckbSub010400_20.Checked.ToString());
                m_Sub010400ALData.Add(rdbSub010400_01.Checked.ToString());
                m_Sub010400ALData.Add(rdbSub010400_02.Checked.ToString());
                m_Sub010400ALData.Add(rdbSub010400_03.Checked.ToString());
                m_Sub010400ALData.Add(rdbSub010400_04.Checked.ToString());

                for (int i = 0; i < DeptCardTree_NodeFun1.m_ALuser_car_group_detailed.Count; i++)
                {
                    m_Sub010400ALData.Add(DeptCardTree_NodeFun1.m_ALuser_car_group_detailed[i].ToString());
                }

                return;
            }
            //--

            if (m_intDB2LeftSub010400_id == -10)//新增
            {
                SQL = String.Format("INSERT INTO user_car_group (name, enable, card_type_enable, card_type, effective_date_enable, effective_date_start, effective_date_end, card_status_enable, card_disable, card_black, card_apb_disable, card_level_enable, card_level, card_week_time_enable, card_week, card_time_period01, card_time_period02, card_time_period03) VALUES ('{0}','{1}','{2}','{3}','{4}','{5}','{6}','{7}','{8}','{9}','{10}','{11}','{12}','{13}','{14}','{15}','{16}','{17}');",
                Strname, Strenable, Strcard_type_enable, Strcard_type, Streffective_date_enable, Streffective_date_start, Streffective_date_end, Strcard_status_enable, Strcard_disable, Strcard_black, Strcard_apb_disable, Strcard_level_enable, Strcard_level, Strcard_week_time_enable, Strcard_week, Strcard_time_period01, Strcard_time_period02, Strcard_time_period03);
            }
            else//修改
            {
                SQL = String.Format("UPDATE user_car_group SET name ='{0}', enable ='{1}', card_type_enable ='{2}', card_type ='{3}', effective_date_enable ='{4}', effective_date_start ='{5}', effective_date_end ='{6}', card_status_enable ='{7}', card_disable ='{8}', card_black ='{9}', card_apb_disable ='{10}', card_level_enable ='{11}', card_level ='{12}', card_week_time_enable ='{13}', card_week ='{14}', card_time_period01 ='{15}', card_time_period02 ='{16}', card_time_period03='{17}' WHERE id={18};",
                Strname, Strenable, Strcard_type_enable, Strcard_type, Streffective_date_enable, Streffective_date_start, Streffective_date_end, Strcard_status_enable, Strcard_disable, Strcard_black, Strcard_apb_disable, Strcard_level_enable, Strcard_level, Strcard_week_time_enable, Strcard_week, Strcard_time_period01, Strcard_time_period02, Strcard_time_period03, m_intDB2LeftSub010400_id);
            }
            MySQL.InsertUpdateDelete(SQL);

            if (m_intDB2LeftSub010400_id == -10)
            {
                SQL = String.Format("SELECT id FROM user_car_group WHERE name='{0}' ORDER BY id DESC;", Strname);
                MySqlDataReader DataReader = MySQL.GetDataReader(SQL);
                while (DataReader.Read())
                {
                    m_intDB2LeftSub010400_id = Convert.ToInt32(DataReader["id"].ToString());
                    break;
                }
                DataReader.Close();
            }

            SQL = String.Format("DELETE FROM user_car_group_detailed WHERE user_car_group_id={0};", m_intDB2LeftSub010400_id);
            MySQL.InsertUpdateDelete(SQL);

            if (DeptCardTree_NodeFun1.m_ALuser_car_group_detailed.Count > 0)
            {
                SQL = "";
                for (int i = 0; i < DeptCardTree_NodeFun1.m_ALuser_car_group_detailed.Count; i++)
                {
                    SQL += String.Format("INSERT INTO user_car_group_detailed (user_car_group_id,card_id,state) VALUES ({0},{1},{2});", m_intDB2LeftSub010400_id, DeptCardTree_NodeFun1.m_ALuser_car_group_detailed[i].ToString(), state);
                    if (((i+1) % 20 == 0))
                    {
                        MySQL.InsertUpdateDelete(SQL);
                        SQL = "";
                    }
                }
                if (SQL.Length > 0)
                {
                    MySQL.InsertUpdateDelete(SQL);
                }
            }
            
        }
        private void butSub010400_03_Click(object sender, EventArgs e)//新增 群組
        {
            if (txtSub010400_01.Text.Length > 0)
            {
                labSub010400_02.ForeColor = Color.Black;

                DeptCardGroupUI2DB();
                initdgvSub010400_01();

                initLeftSub010400UI();
                m_intDB2LeftSub010400_id = -1;
                LeftSub010400UImode();

                initdgvSub0104_01();
                Leave_function();
            }
            else
            {
                labSub010400_02.ForeColor = Color.Red;
                MessageBox.Show(Language.m_StrbutSub010400_03Msg01, butSub010400_03.Text, MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void butSub010400_02_Click(object sender, EventArgs e)//修改/儲存 群組
        {
            if (txtSub010400_01.Text.Length > 0)
            {
                labSub010400_02.ForeColor = Color.Black;

                DeptCardGroupUI2DB();
                initdgvSub010400_01();

                initLeftSub010400UI();
                m_intDB2LeftSub010400_id = -1;
                LeftSub010400UImode();

                initdgvSub0104_01();
                Leave_function();
            }
            else
            {
                labSub010400_02.ForeColor = Color.Red;
                MessageBox.Show(Language.m_StrbutSub010400_02Msg01, butSub010400_02.Text, MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void butSub010400_10_Click(object sender, EventArgs e)//編輯卡片
        {
            Tree_Node tmp_Node = (Tree_Node)tvmSub010400_01.SelectedNode;
            if ((tmp_Node != null) && (tmp_Node.m_tree_level > 1))//if(tmp_Node.m_tree_level > 1)
            {
                m_intcard_id = tmp_Node.m_id;
                modifiedCardData();
            }
        }

        private void butSub010400_20_Click(object sender, EventArgs e)
        {
            DeptCardGroupUI2DB(false);//add at 2017/10/12
            if( (m_intDB2LeftSub010400_id == -1) || CheckUIVarNotChange(m_Sub010400ALInit, m_Sub010400ALData) )//if (m_intDB2LeftSub010400_id == -1)
            {
                initdgvSub0104_01();
                Leave_function();
            }
            else
            {
                DialogResult myResult = MessageBox.Show(Language.m_StrControllerMsg00, butSub010400_03.Text.Trim() + "/" + butSub010400_02.Text.Trim(), MessageBoxButtons.YesNo, MessageBoxIcon.Question);
                if (myResult == DialogResult.Yes)
                {
                    initdgvSub0104_01();
                    Leave_function();
                }
            }
        }
        //Sub010400_end
        //Sub0200_start
        private void butSub0200_01_Click(object sender, EventArgs e)//編輯授權
        {
            try
            {
                int index = dgvSub0200_01.SelectedRows[0].Index;//取得被選取的第一列位置
                String Strid = dgvSub0200_01.Rows[index].Cells[1].Value.ToString();
                m_intdgvSub0200_01_id = Int32.Parse(Strid);

                m_TPOld = m_tabMain.SelectedTab;//--2017/02/22 製作返回按鈕功能
                TabPage_Push();//m_StackTPOld.Push(m_TPOld);//--2017/02/22 製作返回按鈕功能

                m_intDB2LeftSub020000_id = -10;
                initSub020000UI();
                m_tabSub020000.Parent = m_tabMain;

                //--
                //同步子頁的選擇列表 at 2017/07/11
                for (int i = 0; i < dgvSub020000_01.Rows.Count; i++)
                {
                    int id = Convert.ToInt32(dgvSub020000_01.Rows[i].Cells[1].Value.ToString());
                    if (id != m_intdgvSub0200_01_id)
                    {
                        dgvSub020000_01.Rows[i].Selected = false;
                    }
                    else
                    {
                        dgvSub020000_01.Rows[i].Selected = true;
                    }
                }
                //--
                DB2LeftSub020000UI(m_intdgvSub0200_01_id);
                m_tabMain.SelectedTab = m_tabSub020000;

                //--
                //add at 2017/10/13
                m_Sub020000ALInit.Clear();
                m_Sub020000ALInit.Add(txtSub020000_01.Text);
                m_Sub020000ALInit.Add(ckbSub020000_10.Checked.ToString());
                m_Sub020000ALInit.Add(ckbSub020000_11.Checked.ToString());
                m_Sub020000ALInit.Add(ckbSub020000_12.Checked.ToString());
                m_Sub020000ALInit.Add(ckbSub020000_13.Checked.ToString());
                m_Sub020000ALInit.Add(ckbSub020000_14.Checked.ToString());
                m_Sub020000ALInit.Add(ckbSub020000_15.Checked.ToString());
                m_Sub020000ALInit.Add(ckbSub020000_16.Checked.ToString());
                m_Sub020000ALInit.Add(ckbSub020000_17.Checked.ToString());
                m_Sub020000ALInit.Add(ckbSub020000_18.Checked.ToString());
                m_Sub020000ALInit.Add(ckbSub020000_19.Checked.ToString());
                m_Sub020000ALInit.Add(ckbSub020000_20.Checked.ToString());
                m_Sub020000ALInit.Add(ckbSub020000_21.Checked.ToString());
                m_Sub020000ALInit.Add(ckbSub020000_22.Checked.ToString());
                m_Sub020000ALInit.Add(ckbSub020000_23.Checked.ToString());
                m_Sub020000ALInit.Add(ckbSub020000_24.Checked.ToString());
                m_Sub020000ALInit.Add(ckbSub020000_25.Checked.ToString());
                m_Sub020000ALInit.Add(ckbSub020000_26.Checked.ToString());
                m_Sub020000ALInit.Add(rdbSub020000_01.Checked.ToString());
                m_Sub020000ALInit.Add(rdbSub020000_02.Checked.ToString());
                m_Sub020000ALInit.Add(rdbSub020000_03.Checked.ToString());
                m_Sub020000ALInit.Add(rdbSub020000_04.Checked.ToString());
                m_Sub020000ALInit.Add(cmbSub020000_02.SelectedIndex + "");
                m_Sub020000ALInit.Add(adpSub020000_01.Value.ToString("yyyy-MM-dd HH:mm"));
                m_Sub020000ALInit.Add(adpSub020000_02.Value.ToString("yyyy-MM-dd HH:mm"));
                m_Sub020000ALInit.Add(steSub020000_01.StrValue1 + steSub020000_01.StrValue2);
                m_Sub020000ALInit.Add(steSub020000_02.StrValue1 + steSub020000_02.StrValue2);
                m_Sub020000ALInit.Add(steSub020000_03.StrValue1 + steSub020000_03.StrValue2);

                AreaTree_NodeFun AreaTree_NodeFun1 = new AreaTree_NodeFun();
                AreaTree_NodeFun1.getTreeView(tvmSub020000_01, 1);

                DeptCardTree_NodeFun DeptCardTree_NodeFun1 = new DeptCardTree_NodeFun();
                DeptCardTree_NodeFun1.getTreeView(tvmSub020000_02, 1);

                for (int i = 0; i < AreaTree_NodeFun1.m_ALarea_door_group_detailed.Count; i++)
                {
                    m_Sub020000ALInit.Add(AreaTree_NodeFun1.m_ALarea_door_group_detailed[i].ToString());
                }

                for (int i = 0; i < DeptCardTree_NodeFun1.m_ALuser_car_group_detailed.Count; i++)
                {
                    m_Sub020000ALInit.Add(DeptCardTree_NodeFun1.m_ALuser_car_group_detailed[i].ToString());
                }
                //--
            }
            catch
            {

            }
        }

        private void butSub0200_02_Click(object sender, EventArgs e)//新增授權
        {
            m_TPOld = m_tabMain.SelectedTab;//--2017/02/22 製作返回按鈕功能
            TabPage_Push();//m_StackTPOld.Push(m_TPOld);//--2017/02/22 製作返回按鈕功能

            m_intdgvSub0200_01_id = -10;
            m_intDB2LeftSub020000_id = m_intdgvSub0200_01_id;
            initSub020000UI();

            //---
            //新建授權紀錄時，預設"不勾選授權參數啟用"、"授權群組頁面縮起"
            picSub020000_01.Visible = true;

            bgpSub020000_03.Height = 56;

            bgpSub020000_04.Location = new Point(6, 487 - (386 - 56));
            bgpSub020000_05.Location = new Point(402, 487 - (386 - 56));
            bgpSub020000_04.Height = 399 + (386 - 56);
            bgpSub020000_05.Height = 399 + (386 - 56);
            ckbSub020000_10.Checked = false;
            //---新建授權紀錄時，預設"不勾選授權參數啟用"、"授權群組頁面縮起"

            m_tabSub020000.Parent = m_tabMain;
            m_tabMain.SelectedTab = m_tabSub020000;

            //---
            //新增所有群組時都預設填入名稱
            txtSub020000_01.Text = "group_auth_" + DateTime.Now.ToString("yyyyMMddHHmmss");
            //---新增所有群組時都預設填入名稱

            //--
            //add at 2017/10/13
            m_Sub020000ALInit.Clear();
            m_Sub020000ALInit.Add(txtSub020000_01.Text);
            m_Sub020000ALInit.Add(ckbSub020000_10.Checked.ToString());
            m_Sub020000ALInit.Add(ckbSub020000_11.Checked.ToString());
            m_Sub020000ALInit.Add(ckbSub020000_12.Checked.ToString());
            m_Sub020000ALInit.Add(ckbSub020000_13.Checked.ToString());
            m_Sub020000ALInit.Add(ckbSub020000_14.Checked.ToString());
            m_Sub020000ALInit.Add(ckbSub020000_15.Checked.ToString());
            m_Sub020000ALInit.Add(ckbSub020000_16.Checked.ToString());
            m_Sub020000ALInit.Add(ckbSub020000_17.Checked.ToString());
            m_Sub020000ALInit.Add(ckbSub020000_18.Checked.ToString());
            m_Sub020000ALInit.Add(ckbSub020000_19.Checked.ToString());
            m_Sub020000ALInit.Add(ckbSub020000_20.Checked.ToString());
            m_Sub020000ALInit.Add(ckbSub020000_21.Checked.ToString());
            m_Sub020000ALInit.Add(ckbSub020000_22.Checked.ToString());
            m_Sub020000ALInit.Add(ckbSub020000_23.Checked.ToString());
            m_Sub020000ALInit.Add(ckbSub020000_24.Checked.ToString());
            m_Sub020000ALInit.Add(ckbSub020000_25.Checked.ToString());
            m_Sub020000ALInit.Add(ckbSub020000_26.Checked.ToString());
            m_Sub020000ALInit.Add(rdbSub020000_01.Checked.ToString());
            m_Sub020000ALInit.Add(rdbSub020000_02.Checked.ToString());
            m_Sub020000ALInit.Add(rdbSub020000_03.Checked.ToString());
            m_Sub020000ALInit.Add(rdbSub020000_04.Checked.ToString());
            m_Sub020000ALInit.Add(cmbSub020000_02.SelectedIndex + "");
            m_Sub020000ALInit.Add(adpSub020000_01.Value.ToString("yyyy-MM-dd HH:mm"));
            m_Sub020000ALInit.Add(adpSub020000_02.Value.ToString("yyyy-MM-dd HH:mm"));
            m_Sub020000ALInit.Add(steSub020000_01.StrValue1 + steSub020000_01.StrValue2);
            m_Sub020000ALInit.Add(steSub020000_02.StrValue1 + steSub020000_02.StrValue2);
            m_Sub020000ALInit.Add(steSub020000_03.StrValue1 + steSub020000_03.StrValue2);
            //--
        }

        private void butSub0200_06_Click(object sender, EventArgs e)//授權-全選
        {
            /*
            for (int i = 0; i < dgvSub0200_01.Rows.Count; i++)
            {
                dgvSub0200_01.Rows[i].Cells[0].Value = true;
                dgvSub0200_01.Rows[i].Selected = true;
            }
            */
            dgvSub0200_01.SelectAll();//把所有列表元件的全選/取消全選改成SelectAll()/ClearSelection()
        }

        private void butSub0200_07_Click(object sender, EventArgs e)//授權-取消全選
        {
            /*
            for (int i = 0; i < dgvSub0200_01.Rows.Count; i++)
            {
                dgvSub0200_01.Rows[i].Cells[0].Value = false;
                dgvSub0200_01.Rows[i].Selected = false;
            }
            */
            dgvSub0200_01.ClearSelection();//把所有列表元件的全選/取消全選改成SelectAll()/ClearSelection()
        }

        private void dgvSub0200_01_DoubleClick(object sender, EventArgs e)//授權-雙點擊編輯
        {
            butSub0200_01.PerformClick();
        }

        public int m_intdgvSub0200_01_id = -1;
        private void dgvSub0200_01_SelectionChanged(object sender, EventArgs e)
        {
            //---
            //列表元件shift全選時最前面的狀態要能自動打勾，反之要能取消
            for (int i = 0; i < dgvSub0200_01.Rows.Count; i++)
            {
                dgvSub0200_01.Rows[i].Cells[0].Value = false;
            }
            for (int j = 0; j < dgvSub0200_01.SelectedRows.Count; j++)
            {
                dgvSub0200_01.SelectedRows[j].Cells[0].Value = true;
            }
            //---列表元件shift全選時最前面的狀態要能自動打勾，反之要能取消

            try
            {
                int index = dgvSub0200_01.SelectedRows[0].Index;//取得被選取的第一列位置
                String Strid = dgvSub0200_01.Rows[index].Cells[1].Value.ToString();
                m_intdgvSub0200_01_id = Int32.Parse(Strid);
            }
            catch
            {
            }
        }

        private void butSub0200_08_Click(object sender, EventArgs e)//授權-批次處理
        {
            ArrayList ALSN = new ArrayList();
            ALSN.Clear();
            for (int i = 0; i < dgvSub0200_01.Rows.Count; i++)
            {
                String data = dgvSub0200_01.Rows[i].Cells[0].Value.ToString().ToLower();//抓取DataGridView欄位資料
                if (data == "true")
                {
                    ALSN.Add(dgvSub0200_01.Rows[i].Cells[1].Value.ToString());//抓 ID
                }
            }
            String SQL = "";
            switch (cmbSub0200_01.SelectedIndex)
            {
                case 0:
                    SQL = "";
                    for (int i = 0; i < ALSN.Count; i++)
                    {
                        //----
                        //授權批次處理下拉是選單整合新版運算動作流程
                        //SQL += "UPDATE authorization_group SET action = 1,state=1 WHERE (id = " + ALSN[i].ToString() + ");";
                        SQL = "UPDATE authorization_group SET action = 1,state=1 WHERE (id = " + ALSN[i].ToString() + ");";
                        MySQL.InsertUpdateDelete(SQL);
                        authorization_data authorization_data01 = new authorization_data(Convert.ToInt32(ALSN[i].ToString()));
                        ShowAuthorizedParameterAns ShowAuthorizedParameterAns = new ShowAuthorizedParameterAns(authorization_data01);
                        ShowAuthorizedParameterAns.ShowDialog();
                        //---
                        //設定顯示授權運算規則UI 按下OK表示直接建立 授權規則+授權運算，若按下Cancel表示單純建立授權規則
                        if (authorization_data.m_intRun > -1)
                        {
                            //authorization_data01.calculate_saveDB();//修正運算授權結果用Filter方法-運算函數修正
                            //authorization_data.sendData2HW();//整合呼叫傳送授權API的函數
                            Animation.m_ADThreadBuf = authorization_data01;
                            Animation.createThreadAnimation(Language.m_StrAuthorizeStepMsg01, Animation.Thread_authorization);
                        }
                        //---	
                        //----
                    }
                    MessageBox.Show(Language.m_StrAuthTransferMsg02, Language.m_StrAuthTransferMsg01, MessageBoxButtons.OK, MessageBoxIcon.Information);
                    break;
                case 1:
                    SQL = "";
                    for (int i = 0; i < ALSN.Count; i++)
                    {
                        //----
                        //授權批次處理下拉是選單整合新版運算動作流程
                        //SQL += "UPDATE authorization_group SET action = 0,state=1 WHERE (id = " + ALSN[i].ToString() + ");";
                        SQL = "UPDATE authorization_group SET action = 0,state=1 WHERE (id = " + ALSN[i].ToString() + ");";
                        MySQL.InsertUpdateDelete(SQL);
                        authorization_data authorization_data01 = new authorization_data(Convert.ToInt32(ALSN[i].ToString()));
                        ShowAuthorizedParameterAns ShowAuthorizedParameterAns = new ShowAuthorizedParameterAns(authorization_data01);
                        ShowAuthorizedParameterAns.ShowDialog();
                        //---
                        //設定顯示授權運算規則UI 按下OK表示直接建立 授權規則+授權運算，若按下Cancel表示單純建立授權規則
                        if (authorization_data.m_intRun > -1)
                        {
                            //authorization_data01.calculate_saveDB();//修正運算授權結果用Filter方法-運算函數修正
                            //authorization_data.sendData2HW();//整合呼叫傳送授權API的函數
                            Animation.m_ADThreadBuf = authorization_data01;
                            Animation.createThreadAnimation(Language.m_StrAuthorizeStepMsg01, Animation.Thread_authorization);
                        }
                        //---
                        //----
                    }
                    MessageBox.Show(Language.m_StrAuthTransferMsg02, Language.m_StrAuthTransferMsg01, MessageBoxButtons.OK, MessageBoxIcon.Information);
                    break;
                case 2:
                    SQL = "";
                    for (int i = 0; i < ALSN.Count; i++)
                    {
                        SQL += String.Format("DELETE FROM authorization_group WHERE id={0};DELETE FROM authorization_group_detailed WHERE authorization_group_id={0};", ALSN[i].ToString());
                    }
                    MySQL.InsertUpdateDelete(SQL);//授權批次處理下拉是選單整合新版運算動作流程
                    break;
                case 3:
                    //---------------------
                    //修改授權運算時間點-只有在執行批次處理『Applied』
                    for (int i = 0; i < ALSN.Count; i++)
                    {
                        authorization_data authorization_data01 = new authorization_data(Convert.ToInt32(ALSN[i].ToString()));
                        //authorization_data01.calculate_saveDB();//修正運算授權結果用Filter方法-運算函數修正
                        //authorization_data.sendData2HW();//整合呼叫傳送授權API的函數
                        Animation.m_ADThreadBuf = authorization_data01;
                        Animation.createThreadAnimation(Language.m_StrAuthorizeStepMsg01, Animation.Thread_authorization);
                    }
                    MessageBox.Show(Language.m_StrAuthTransferMsg02, Language.m_StrAuthTransferMsg01, MessageBoxButtons.OK, MessageBoxIcon.Information);
                    //---------------------
                    break;
            }


            initdgvSub0200_01();
        }

        private void butSub0200_09_Click(object sender, EventArgs e)//授權-頁面搜尋
        {
            initdgvSub0200_01();
            ArrayList AL01 = new ArrayList();
            ArrayList AL02 = new ArrayList();
            ArrayList AL03 = new ArrayList();
            ArrayList AL04 = new ArrayList();
            ArrayList AL05 = new ArrayList();
            AL01.Clear();
            AL02.Clear();
            AL03.Clear();
            AL04.Clear();
            AL05.Clear();

            if (txtSub0200_01.Text != "")
            {
                for (int i = 0; i < dgvSub0200_01.Rows.Count; i++)//取的現行UI上控制器列表所有資料
                {
                    AL01.Add(dgvSub0200_01.Rows[i].Cells[1].Value.ToString());
                    AL02.Add(dgvSub0200_01.Rows[i].Cells[2].Value.ToString());
                    AL03.Add(dgvSub0200_01.Rows[i].Cells[3].Value.ToString());
                    AL04.Add(dgvSub0200_01.Rows[i].Cells[4].Value.ToString());
                    AL05.Add(dgvSub0200_01.Rows[i].Cells[5].Value.ToString());
                }
                try
                {
                    //--
                    //dgvSub0200_01.ReadOnly = true;//唯讀 不可更改
                    dgvSub0200_01.RowHeadersVisible = false;//DataGridView 最前面指示選取列所在位置的箭頭欄位
                    dgvSub0200_01.Rows[0].Selected = false;//取消DataGridView的默認選取(選中)Cell 使其不反藍
                    dgvSub0200_01.AllowUserToAddRows = false;//是否允許使用者新增資料
                    dgvSub0200_01.AllowUserToDeleteRows = false;//是否允許使用者刪除資料
                    dgvSub0200_01.AllowUserToOrderColumns = false;//是否允許使用者調整欄位位置
                    //所有表格欄位寬度全部變成可調 dgvSub0200_01.AllowUserToResizeColumns = false;//是否允許使用者改變欄寬
                    dgvSub0200_01.AllowUserToResizeRows = false;//是否允許使用者改變行高
                    dgvSub0200_01.Columns[1].ReadOnly = true;//單一欄位禁止編輯
                    dgvSub0200_01.Columns[2].ReadOnly = true;//單一欄位禁止編輯
                    dgvSub0200_01.Columns[3].ReadOnly = true;//單一欄位禁止編輯
                    dgvSub0200_01.Columns[4].ReadOnly = true;//單一欄位禁止編輯
                    dgvSub0200_01.Columns[5].ReadOnly = true;//單一欄位禁止編輯
                    dgvSub0200_01.AllowUserToAddRows = false;//刪除空白列
                    dgvSub0200_01.SelectionMode = System.Windows.Forms.DataGridViewSelectionMode.FullRowSelect;//整列選取
                    //--

                    do
                    {
                        for (int i = 0; i < dgvSub0200_01.Rows.Count; i++)
                        {
                            DataGridViewRow r1 = this.dgvSub0200_01.Rows[i];//取得DataGridView整列資料
                            this.dgvSub0200_01.Rows.Remove(r1);//DataGridView刪除整列
                        }
                    } while (dgvSub0200_01.Rows.Count > 0);

                }
                catch
                {
                }
                String StrSearch = txtSub0200_01.Text;
                for (int i = 0; i < AL01.Count; i++)
                {
                    //AL01[i].ToString()->DB index 本來就被隱藏 所以不用在搜尋欄位內
                    if ((AL02[i].ToString().IndexOf(StrSearch) > -1) || (AL03[i].ToString().IndexOf(StrSearch) > -1) || (AL04[i].ToString().IndexOf(StrSearch) > -1) || (AL05[i].ToString().IndexOf(StrSearch) > -1))
                    {
                        this.dgvSub0200_01.Rows.Add(false, AL01[i].ToString(), AL02[i].ToString(), AL03[i].ToString(), AL04[i].ToString(), AL05[i].ToString());
                    }
                }
            }
        }

        private void ckbSub0200_01_CheckedChanged(object sender, EventArgs e)
        {
            m_StrdgvSub0200_01_ext01 = "";
            if (ckbSub0200_01.Checked)
            {
                if (ckbSub0200_02.Checked)//(1,1)
                {
                    m_StrdgvSub0200_01_ext01 = "";//兩個都選等於沒選
                }
                else//(1,0)
                {
                    m_StrdgvSub0200_01_ext01 = " WHERE action = 1";
                }
            }
            else
            {
                if (ckbSub0200_02.Checked)//(0,1)
                {
                    m_StrdgvSub0200_01_ext01 = " WHERE action = 0";
                }
                else//(0,0)
                {
                    m_StrdgvSub0200_01_ext01 = "";//沒選
                }
            }
            initdgvSub0200_01();
        }

        //Sub0200_end
        //Sub020000_start
        private void butSub020000_20_Click(object sender, EventArgs e)//授權列表-全選
        {
            /*
            for (int i = 0; i < dgvSub020000_01.Rows.Count; i++)
            {
                dgvSub020000_01.Rows[i].Cells[0].Value = true;
                dgvSub020000_01.Rows[i].Selected = true;
            }
            */
            dgvSub020000_01.SelectAll();//把所有列表元件的全選/取消全選改成SelectAll()/ClearSelection()
        }

        private void butSub020000_21_Click(object sender, EventArgs e)//授權列表-取消全選
        {
            /*
            for (int i = 0; i < dgvSub020000_01.Rows.Count; i++)
            {
                dgvSub020000_01.Rows[i].Cells[0].Value = false;
                dgvSub020000_01.Rows[i].Selected = false;
            }
            */
            dgvSub020000_01.ClearSelection();//把所有列表元件的全選/取消全選改成SelectAll()/ClearSelection()
        }

        private void butSub020000_16_Click(object sender, EventArgs e)//新增授權
        {
            m_intDB2LeftSub020000_id = -10;
            DB2LeftSub020000UI(m_intDB2LeftSub020000_id);

            //---
            //新建授權紀錄時，預設"不勾選授權參數啟用"、"授權群組頁面縮起"
            picSub020000_01.Visible = true;

            bgpSub020000_03.Height = 56;

            bgpSub020000_04.Location = new Point(6, 487 - (386 - 56));
            bgpSub020000_05.Location = new Point(402, 487 - (386 - 56));
            bgpSub020000_04.Height = 399 + (386 - 56);
            bgpSub020000_05.Height = 399 + (386 - 56);
            ckbSub020000_10.Checked = false;
            //---新建授權紀錄時，預設"不勾選授權參數啟用"、"授權群組頁面縮起"

            //--
            //add at 2017/10/13
            m_Sub020000ALInit.Clear();
            m_Sub020000ALInit.Add(txtSub020000_01.Text);
            m_Sub020000ALInit.Add(ckbSub020000_10.Checked.ToString());
            m_Sub020000ALInit.Add(ckbSub020000_11.Checked.ToString());
            m_Sub020000ALInit.Add(ckbSub020000_12.Checked.ToString());
            m_Sub020000ALInit.Add(ckbSub020000_13.Checked.ToString());
            m_Sub020000ALInit.Add(ckbSub020000_14.Checked.ToString());
            m_Sub020000ALInit.Add(ckbSub020000_15.Checked.ToString());
            m_Sub020000ALInit.Add(ckbSub020000_16.Checked.ToString());
            m_Sub020000ALInit.Add(ckbSub020000_17.Checked.ToString());
            m_Sub020000ALInit.Add(ckbSub020000_18.Checked.ToString());
            m_Sub020000ALInit.Add(ckbSub020000_19.Checked.ToString());
            m_Sub020000ALInit.Add(ckbSub020000_20.Checked.ToString());
            m_Sub020000ALInit.Add(ckbSub020000_21.Checked.ToString());
            m_Sub020000ALInit.Add(ckbSub020000_22.Checked.ToString());
            m_Sub020000ALInit.Add(ckbSub020000_23.Checked.ToString());
            m_Sub020000ALInit.Add(ckbSub020000_24.Checked.ToString());
            m_Sub020000ALInit.Add(ckbSub020000_25.Checked.ToString());
            m_Sub020000ALInit.Add(ckbSub020000_26.Checked.ToString());
            m_Sub020000ALInit.Add(rdbSub020000_01.Checked.ToString());
            m_Sub020000ALInit.Add(rdbSub020000_02.Checked.ToString());
            m_Sub020000ALInit.Add(rdbSub020000_03.Checked.ToString());
            m_Sub020000ALInit.Add(rdbSub020000_04.Checked.ToString());
            m_Sub020000ALInit.Add(cmbSub020000_02.SelectedIndex + "");
            m_Sub020000ALInit.Add(adpSub020000_01.Value.ToString("yyyy-MM-dd HH:mm"));
            m_Sub020000ALInit.Add(adpSub020000_02.Value.ToString("yyyy-MM-dd HH:mm"));
            m_Sub020000ALInit.Add(steSub020000_01.StrValue1 + steSub020000_01.StrValue2);
            m_Sub020000ALInit.Add(steSub020000_02.StrValue1 + steSub020000_02.StrValue2);
            m_Sub020000ALInit.Add(steSub020000_03.StrValue1 + steSub020000_03.StrValue2);
            //--
        }

        private void butSub020000_15_Click(object sender, EventArgs e)//編輯授權
        {
            int intbuf = -1;
            try
            {
                int index = dgvSub020000_01.SelectedRows[0].Index;//取得被選取的第一列位置
                String Strid = dgvSub020000_01.Rows[index].Cells[1].Value.ToString();
                intbuf = Int32.Parse(Strid);
            }
            catch
            {
            }
            if (intbuf > 0)
            {
                //m_intDB2LeftSub020000_id = -10;
                initLeftSub020000UI();//initSub020000UI();

                m_intDB2LeftSub020000_id = intbuf;
                DB2LeftSub020000UI(m_intDB2LeftSub020000_id);

                //--
                //add at 2017/10/13
                m_Sub020000ALInit.Clear();
                m_Sub020000ALInit.Add(txtSub020000_01.Text);
                m_Sub020000ALInit.Add(ckbSub020000_10.Checked.ToString());
                m_Sub020000ALInit.Add(ckbSub020000_11.Checked.ToString());
                m_Sub020000ALInit.Add(ckbSub020000_12.Checked.ToString());
                m_Sub020000ALInit.Add(ckbSub020000_13.Checked.ToString());
                m_Sub020000ALInit.Add(ckbSub020000_14.Checked.ToString());
                m_Sub020000ALInit.Add(ckbSub020000_15.Checked.ToString());
                m_Sub020000ALInit.Add(ckbSub020000_16.Checked.ToString());
                m_Sub020000ALInit.Add(ckbSub020000_17.Checked.ToString());
                m_Sub020000ALInit.Add(ckbSub020000_18.Checked.ToString());
                m_Sub020000ALInit.Add(ckbSub020000_19.Checked.ToString());
                m_Sub020000ALInit.Add(ckbSub020000_20.Checked.ToString());
                m_Sub020000ALInit.Add(ckbSub020000_21.Checked.ToString());
                m_Sub020000ALInit.Add(ckbSub020000_22.Checked.ToString());
                m_Sub020000ALInit.Add(ckbSub020000_23.Checked.ToString());
                m_Sub020000ALInit.Add(ckbSub020000_24.Checked.ToString());
                m_Sub020000ALInit.Add(ckbSub020000_25.Checked.ToString());
                m_Sub020000ALInit.Add(ckbSub020000_26.Checked.ToString());
                m_Sub020000ALInit.Add(rdbSub020000_01.Checked.ToString());
                m_Sub020000ALInit.Add(rdbSub020000_02.Checked.ToString());
                m_Sub020000ALInit.Add(rdbSub020000_03.Checked.ToString());
                m_Sub020000ALInit.Add(rdbSub020000_04.Checked.ToString());
                m_Sub020000ALInit.Add(cmbSub020000_02.SelectedIndex + "");
                m_Sub020000ALInit.Add(adpSub020000_01.Value.ToString("yyyy-MM-dd HH:mm"));
                m_Sub020000ALInit.Add(adpSub020000_02.Value.ToString("yyyy-MM-dd HH:mm"));
                m_Sub020000ALInit.Add(steSub020000_01.StrValue1 + steSub020000_01.StrValue2);
                m_Sub020000ALInit.Add(steSub020000_02.StrValue1 + steSub020000_02.StrValue2);
                m_Sub020000ALInit.Add(steSub020000_03.StrValue1 + steSub020000_03.StrValue2);

                AreaTree_NodeFun AreaTree_NodeFun1 = new AreaTree_NodeFun();
                AreaTree_NodeFun1.getTreeView(tvmSub020000_01, 1);

                DeptCardTree_NodeFun DeptCardTree_NodeFun1 = new DeptCardTree_NodeFun();
                DeptCardTree_NodeFun1.getTreeView(tvmSub020000_02, 1);

                for (int i = 0; i < AreaTree_NodeFun1.m_ALarea_door_group_detailed.Count; i++)
                {
                    m_Sub020000ALInit.Add(AreaTree_NodeFun1.m_ALarea_door_group_detailed[i].ToString());
                }

                for (int i = 0; i < DeptCardTree_NodeFun1.m_ALuser_car_group_detailed.Count; i++)
                {
                    m_Sub020000ALInit.Add(DeptCardTree_NodeFun1.m_ALuser_car_group_detailed[i].ToString());
                }
                //--
            }
        }

        private void butSub020000_23_Click(object sender, EventArgs e)//授權列表-搜尋
        {
            initdgvSub020000_01();
            ArrayList AL01 = new ArrayList();
            ArrayList AL02 = new ArrayList();
            ArrayList AL03 = new ArrayList();
            ArrayList AL04 = new ArrayList();
            ArrayList AL05 = new ArrayList();
            AL01.Clear();
            AL02.Clear();
            AL03.Clear();
            AL04.Clear();
            AL05.Clear();

            if (txtSub020000_04.Text != "")
            {
                for (int i = 0; i < dgvSub020000_01.Rows.Count; i++)//取的現行UI上控制器列表所有資料
                {
                    AL01.Add(dgvSub020000_01.Rows[i].Cells[1].Value.ToString());
                    AL02.Add(dgvSub020000_01.Rows[i].Cells[2].Value.ToString());
                    AL03.Add(dgvSub020000_01.Rows[i].Cells[3].Value.ToString());
                    AL04.Add(dgvSub020000_01.Rows[i].Cells[4].Value.ToString());
                    AL05.Add(dgvSub020000_01.Rows[i].Cells[5].Value.ToString());
                }
                try
                {
                    //--
                    //dgvSub020000_01.ReadOnly = true;//唯讀 不可更改
                    dgvSub020000_01.RowHeadersVisible = false;//DataGridView 最前面指示選取列所在位置的箭頭欄位
                    dgvSub020000_01.Rows[0].Selected = false;//取消DataGridView的默認選取(選中)Cell 使其不反藍
                    dgvSub020000_01.AllowUserToAddRows = false;//是否允許使用者新增資料
                    dgvSub020000_01.AllowUserToDeleteRows = false;//是否允許使用者刪除資料
                    dgvSub020000_01.AllowUserToOrderColumns = false;//是否允許使用者調整欄位位置
                    //所有表格欄位寬度全部變成可調 dgvSub020000_01.AllowUserToResizeColumns = false;//是否允許使用者改變欄寬
                    dgvSub020000_01.AllowUserToResizeRows = false;//是否允許使用者改變行高
                    dgvSub020000_01.Columns[1].ReadOnly = true;//單一欄位禁止編輯
                    dgvSub020000_01.Columns[2].ReadOnly = true;//單一欄位禁止編輯
                    dgvSub020000_01.Columns[3].ReadOnly = true;//單一欄位禁止編輯
                    dgvSub020000_01.Columns[4].ReadOnly = true;//單一欄位禁止編輯
                    dgvSub020000_01.Columns[5].ReadOnly = true;//單一欄位禁止編輯
                    dgvSub020000_01.AllowUserToAddRows = false;//刪除空白列
                    dgvSub020000_01.SelectionMode = System.Windows.Forms.DataGridViewSelectionMode.FullRowSelect;//整列選取
                    //--

                    do
                    {
                        for (int i = 0; i < dgvSub020000_01.Rows.Count; i++)
                        {
                            DataGridViewRow r1 = this.dgvSub020000_01.Rows[i];//取得DataGridView整列資料
                            this.dgvSub020000_01.Rows.Remove(r1);//DataGridView刪除整列
                        }
                    } while (dgvSub020000_01.Rows.Count > 0);

                }
                catch
                {
                }
                String StrSearch = txtSub020000_04.Text;
                for (int i = 0; i < AL01.Count; i++)
                {
                    //AL01[i].ToString()->DB index 本來就被隱藏 所以不用在搜尋欄位內
                    if ((AL02[i].ToString().IndexOf(StrSearch) > -1) || (AL03[i].ToString().IndexOf(StrSearch) > -1) || (AL04[i].ToString().IndexOf(StrSearch) > -1) || (AL05[i].ToString().IndexOf(StrSearch) > -1))
                    {
                        this.dgvSub020000_01.Rows.Add(false, AL01[i].ToString(), AL02[i].ToString(), AL03[i].ToString(), AL04[i].ToString(), AL05[i].ToString());
                    }
                }
            }
        }

        private void ckbSub020000_10_CheckedChanged(object sender, EventArgs e)//群組參數啟用
        {
            if (ckbSub020000_10.Checked == true)
            {
                ckbSub020000_11.Enabled = true;
                ckbSub020000_12.Enabled = true;
                ckbSub020000_13.Enabled = true;
                ckbSub020000_14.Enabled = true;
                ckbSub020000_15.Enabled = true;
            }
            else
            {
                ckbSub020000_11.Enabled = false;
                ckbSub020000_12.Enabled = false;
                ckbSub020000_13.Enabled = false;
                ckbSub020000_14.Enabled = false;
                ckbSub020000_15.Enabled = false;

                //--
                //add at 2017/10/17
                ckbSub020000_11.Checked = false;
                ckbSub020000_12.Checked = false;
                ckbSub020000_13.Checked = false;
                ckbSub020000_14.Checked = false;
                ckbSub020000_15.Checked = false;
                //--
            }
        }

        private void ckbSub020000_11_CheckedChanged(object sender, EventArgs e)//設置卡片類型
        {
            if (ckbSub020000_11.Checked == true)
            {
                cmbSub020000_02.Enabled = true;

                //--
                //把卡片UI預設變成一般卡(原本是空值，變相要使用者一定要編輯)
                if (cmbSub020000_02.SelectedIndex == -1)
                {
                    cmbSub020000_02.SelectedIndex = 0;
                }
                //--
            }
            else
            {
                cmbSub020000_02.Enabled = false;

                cmbSub020000_02.SelectedIndex = -1;//把卡片UI預設變成一般卡(原本是空值，變相要使用者一定要編輯)
            }
        }

        private void ckbSub020000_12_CheckedChanged(object sender, EventArgs e)//設置有效期
        {
            if (ckbSub020000_12.Checked == true)
            {
                adpSub020000_01.Enabled = true;
                adpSub020000_02.Enabled = true;
            }
            else
            {
                adpSub020000_01.Enabled = false;
                adpSub020000_02.Enabled = false;
            }
        }

        private void ckbSub020000_14_CheckedChanged(object sender, EventArgs e)//設置卡片狀態
        {
            if (ckbSub020000_14.Checked == true)
            {
                ckbSub020000_16.Enabled = true;
                ckbSub020000_17.Enabled = true;
                ckbSub020000_18.Enabled = true;
            }
            else
            {
                ckbSub020000_16.Enabled = false;
                ckbSub020000_17.Enabled = false;
                ckbSub020000_18.Enabled = false;
            }
        }

        private void ckbSub020000_15_CheckedChanged(object sender, EventArgs e)//設置通行等級
        {
            if (ckbSub020000_15.Checked == true)
            {
                rdbSub020000_02.Checked = true;

                rdbSub020000_01.Enabled = true;
                rdbSub020000_02.Enabled = true;
                rdbSub020000_03.Enabled = true;
                rdbSub020000_04.Enabled = true;
            }
            else
            {
                rdbSub020000_01.Checked = false;
                rdbSub020000_02.Checked = false;
                rdbSub020000_03.Checked = false;
                rdbSub020000_04.Checked = false;

                rdbSub020000_01.Enabled = false;
                rdbSub020000_02.Enabled = false;
                rdbSub020000_03.Enabled = false;
                rdbSub020000_04.Enabled = false;
            }
        }

        private void ckbSub020000_13_CheckedChanged(object sender, EventArgs e)//設置週計畫與通行時段
        {
            if (ckbSub020000_13.Checked == true)
            {
                ckbSub020000_19.Enabled = true;
                ckbSub020000_20.Enabled = true;
                ckbSub020000_21.Enabled = true;
                ckbSub020000_22.Enabled = true;
                ckbSub020000_23.Enabled = true;
                ckbSub020000_24.Enabled = true;
                ckbSub020000_25.Enabled = true;
                ckbSub020000_26.Enabled = true;
                steSub020000_01.Enabled = true;
                steSub020000_02.Enabled = true;
                steSub020000_03.Enabled = true;
            }
            else
            {
                ckbSub020000_19.Enabled = false;
                ckbSub020000_20.Enabled = false;
                ckbSub020000_21.Enabled = false;
                ckbSub020000_22.Enabled = false;
                ckbSub020000_23.Enabled = false;
                ckbSub020000_24.Enabled = false;
                ckbSub020000_25.Enabled = false;
                ckbSub020000_26.Enabled = false;
                steSub020000_01.Enabled = false;
                steSub020000_02.Enabled = false;
                steSub020000_03.Enabled = false;
            }
        }

        private void butSub020000_06_Click(object sender, EventArgs e)//區域門區-全選 ~TreeView 全選
        {
            for (int i = 0; i < tvmSub020000_01.Nodes.Count; i++)
            {
                Tree_Node tmp = ((Tree_Node)tvmSub020000_01.Nodes[i]);
                tmp.Checked = true;
                AreaTree_NodeFun.SetChildNodeCheckedState(tmp, tmp.Checked);
            }
        }

        private void butSub020000_07_Click(object sender, EventArgs e)//區域門區-取消全選 ~TreeView 取消全選
        {
            for (int i = 0; i < tvmSub020000_01.Nodes.Count; i++)
            {
                Tree_Node tmp = ((Tree_Node)tvmSub020000_01.Nodes[i]);
                tmp.Checked = false;
                AreaTree_NodeFun.SetChildNodeCheckedState(tmp, tmp.Checked);
            }
        }

        private void butSub020000_11_Click(object sender, EventArgs e)//部門人員車輛-全選 ~TreeView 全選
        {
            for (int i = 0; i < tvmSub020000_02.Nodes.Count; i++)
            {
                Tree_Node tmp = ((Tree_Node)tvmSub020000_02.Nodes[i]);
                tmp.Checked = true;
                DeptCardTree_NodeFun.SetChildNodeCheckedState(tmp, tmp.Checked);
            }
        }

        private void butSub020000_12_Click(object sender, EventArgs e)//部門人員車輛-取消全選 ~TreeView 取消全選
        {
            for (int i = 0; i < tvmSub020000_02.Nodes.Count; i++)
            {
                Tree_Node tmp = ((Tree_Node)tvmSub020000_02.Nodes[i]);
                tmp.Checked = false;
                DeptCardTree_NodeFun.SetChildNodeCheckedState(tmp, tmp.Checked);
            }
        }

        private void butSub020000_22_Click(object sender, EventArgs e)
        {
            ArrayList ALSN = new ArrayList();
            ALSN.Clear();
            for (int i = 0; i < dgvSub020000_01.Rows.Count; i++)
            {
                String data = dgvSub020000_01.Rows[i].Cells[0].Value.ToString().ToLower();//抓取DataGridView欄位資料
                if (data == "true")
                {
                    ALSN.Add(dgvSub020000_01.Rows[i].Cells[1].Value.ToString());//抓 ID
                }
            }
            String SQL = "";
            switch (cmbSub020000_01.SelectedIndex)
            {
                case 0:
                    SQL = "";
                    for (int i = 0; i < ALSN.Count; i++)
                    {
                        //----
                        //授權批次處理下拉是選單整合新版運算動作流程
                        //SQL += "UPDATE authorization_group SET action = 1,state=1 WHERE (id = " + ALSN[i].ToString() + ");";
                        SQL = "UPDATE authorization_group SET action = 1,state=1 WHERE (id = " + ALSN[i].ToString() + ");";
                        MySQL.InsertUpdateDelete(SQL);
                        authorization_data authorization_data01 = new authorization_data(Convert.ToInt32(ALSN[i].ToString()));
                        ShowAuthorizedParameterAns ShowAuthorizedParameterAns = new ShowAuthorizedParameterAns(authorization_data01);
                        ShowAuthorizedParameterAns.ShowDialog();
                        //---
                        //設定顯示授權運算規則UI 按下OK表示直接建立 授權規則+授權運算，若按下Cancel表示單純建立授權規則
                        if (authorization_data.m_intRun > -1)
                        {
                            //authorization_data01.calculate_saveDB();//修正運算授權結果用Filter方法-運算函數修正
                            //authorization_data.sendData2HW();//整合呼叫傳送授權API的函數
                            Animation.m_ADThreadBuf = authorization_data01;
                            Animation.createThreadAnimation(Language.m_StrAuthorizeStepMsg01, Animation.Thread_authorization);
                        }
                        //---	
                        //----
                    }
                    MessageBox.Show(Language.m_StrAuthTransferMsg02, Language.m_StrAuthTransferMsg01, MessageBoxButtons.OK, MessageBoxIcon.Information);
                    break;
                case 1:
                    SQL = "";
                    for (int i = 0; i < ALSN.Count; i++)
                    {
                        //----
                        //授權批次處理下拉是選單整合新版運算動作流程
                        //SQL += "UPDATE authorization_group SET action = 0,state=1 WHERE (id = " + ALSN[i].ToString() + ");";
                        SQL = "UPDATE authorization_group SET action = 0,state=1 WHERE (id = " + ALSN[i].ToString() + ");";
                        MySQL.InsertUpdateDelete(SQL);
                        authorization_data authorization_data01 = new authorization_data(Convert.ToInt32(ALSN[i].ToString()));
                        ShowAuthorizedParameterAns ShowAuthorizedParameterAns = new ShowAuthorizedParameterAns(authorization_data01);
                        ShowAuthorizedParameterAns.ShowDialog();
                        //---
                        //設定顯示授權運算規則UI 按下OK表示直接建立 授權規則+授權運算，若按下Cancel表示單純建立授權規則
                        if (authorization_data.m_intRun > -1)
                        {
                            //authorization_data01.calculate_saveDB();//修正運算授權結果用Filter方法-運算函數修正
                            //authorization_data.sendData2HW();//整合呼叫傳送授權API的函數
                            Animation.m_ADThreadBuf = authorization_data01;
                            Animation.createThreadAnimation(Language.m_StrAuthorizeStepMsg01, Animation.Thread_authorization);
                        }
                        //---
                        //----
                    }
                    MessageBox.Show(Language.m_StrAuthTransferMsg02, Language.m_StrAuthTransferMsg01, MessageBoxButtons.OK, MessageBoxIcon.Information);
                    break;
                case 2:
                    SQL = "";
                    for (int i = 0; i < ALSN.Count; i++)
                    {
                        SQL += String.Format("DELETE FROM authorization_group WHERE id={0};DELETE FROM authorization_group_detailed WHERE authorization_group_id={0};", ALSN[i].ToString());
                    }
                    MySQL.InsertUpdateDelete(SQL);//授權批次處理下拉是選單整合新版運算動作流程
                    break;
                case 3:
                    //---------------------
                    // 修改授權運算時間點-只有在執行批次處理『Applied』
                    for (int i = 0; i < ALSN.Count; i++)
                    {
                        authorization_data authorization_data01 = new authorization_data(Convert.ToInt32(ALSN[i].ToString()));
                        //authorization_data01.calculate_saveDB();//修正運算授權結果用Filter方法-運算函數修正
                        //authorization_data.sendData2HW();//整合呼叫傳送授權API的函數
                        Animation.m_ADThreadBuf = authorization_data01;
                        Animation.createThreadAnimation(Language.m_StrAuthorizeStepMsg01, Animation.Thread_authorization);
                    }
                    MessageBox.Show(Language.m_StrAuthTransferMsg02, Language.m_StrAuthTransferMsg01, MessageBoxButtons.OK, MessageBoxIcon.Information);
                    //---------------------
                    break;
            }

            initdgvSub020000_01();

            initLeftSub020000UI();
            m_intDB2LeftSub020000_id = -1;
            LeftSub020000UImode();
        }

        private void butSub020000_24_Click(object sender, EventArgs e)//離開
        {
            AuthorizationGroupUI2DB(false);//add at 2017/10/13
            if( (m_intDB2LeftSub020000_id == -1) || CheckUIVarNotChange(m_Sub020000ALInit, m_Sub020000ALData) )//if (m_intDB2LeftSub020000_id == -1)
            {
                initdgvSub0200_01();
                Leave_function();
            }
            else
            {
                DialogResult myResult = MessageBox.Show(Language.m_StrControllerMsg00, butSub020000_16.Text.Trim() + "/" + butSub020000_15.Text.Trim(), MessageBoxButtons.YesNo, MessageBoxIcon.Question);
                if (myResult == DialogResult.Yes)
                {
                    initdgvSub0200_01();
                    Leave_function();
                }
            }
        }

        public int AuthorizationGroupUI2DB(bool blnRunSQL=true,int state = 1)
        {
            String Strname, Strenable, Strcard_type_enable, Strcard_type, Streffective_date_enable, Streffective_date_start, Streffective_date_end, Strcard_status_enable, Strcard_disable, Strcard_black, Strcard_apb_disable, Strcard_level_enable, Strcard_level, Strcard_week_time_enable, Strcard_week, Strcard_time_period01, Strcard_time_period02, Strcard_time_period03;
            String SQL = "";
            bool blnNotRepeat01 = true, blnNotRepeat02 = true;

            Strname = txtSub020000_01.Text;
            Strenable = "0";
            Strcard_type_enable = "0";
            Strcard_type = "-10";
            Streffective_date_enable = "0";
            Strcard_status_enable = "0";
            Strcard_disable = "0";
            Strcard_black = "0";
            Strcard_apb_disable = "0";
            Strcard_level_enable = "0";
            Strcard_level = "-1";
            Strcard_week_time_enable = "0";
            Strcard_week = "0";

            if (ckbSub020000_10.Checked == true)
            {
                Strenable = "1";
            }

            if (ckbSub020000_11.Checked == true)
            {
                Strcard_type_enable = "1";
            }

            if (cmbSub020000_02.SelectedIndex >= 0)
            {
                Strcard_type = m_ALCardType_ID[cmbSub020000_02.SelectedIndex].ToString();
            }

            if (ckbSub020000_12.Checked == true)
            {
                Streffective_date_enable = "1";
            }

            Streffective_date_start = adpSub020000_01.Value.ToString("yyyy/MM/dd HH:mm");
            Streffective_date_end = adpSub020000_02.Value.ToString("yyyy/MM/dd HH:mm");

            if (ckbSub020000_14.Checked == true)
            {
                Strcard_status_enable = "1";
            }

            if (ckbSub020000_16.Checked == true)
            {
                Strcard_disable = "1";
            }
            if (ckbSub020000_17.Checked == true)
            {
                Strcard_black = "1";
            }

            if (ckbSub020000_18.Checked == true)
            {
                Strcard_apb_disable = "1";
            }

            if (ckbSub020000_15.Checked == true)
            {
                Strcard_level_enable = "1";
            }

            if (rdbSub020000_01.Checked == true)
            {
                Strcard_level = "0";
            }
            else if (rdbSub020000_02.Checked == true)
            {
                Strcard_level = "1";
            }
            else if (rdbSub020000_03.Checked == true)
            {
                Strcard_level = "2";
            }
            else if (rdbSub020000_04.Checked == true)
            {
                Strcard_level = "3";
            }

            if (ckbSub020000_13.Checked == true)
            {
                Strcard_week_time_enable = "1";
            }

            int v1 = 0;
            v1 += Convert.ToInt32(ckbSub020000_19.Checked) * 1;
            v1 += Convert.ToInt32(ckbSub020000_20.Checked) * 2;
            v1 += Convert.ToInt32(ckbSub020000_21.Checked) * 4;
            v1 += Convert.ToInt32(ckbSub020000_22.Checked) * 8;
            v1 += Convert.ToInt32(ckbSub020000_23.Checked) * 16;
            v1 += Convert.ToInt32(ckbSub020000_24.Checked) * 32;
            v1 += Convert.ToInt32(ckbSub020000_25.Checked) * 64;
            v1 += Convert.ToInt32(ckbSub020000_26.Checked) * 128;
            Strcard_week = v1 + "";

            Strcard_time_period01 = steSub020000_01.StrValue1 + "~" + steSub020000_01.StrValue2;

            Strcard_time_period02 = steSub020000_02.StrValue1 + "~" + steSub020000_02.StrValue2;

            Strcard_time_period03 = steSub020000_03.StrValue1 + "~" + steSub020000_03.StrValue2;

            AreaTree_NodeFun AreaTree_NodeFun1 = new AreaTree_NodeFun();
            AreaTree_NodeFun1.getTreeView(tvmSub020000_01, 1);
            blnNotRepeat01 = true;
            blnNotRepeat01 = CheckDBObjectNotRepeat(AreaTree_NodeFun1.m_ALarea_door_group_detailed, 0);//mdel 0->door,1->card

            DeptCardTree_NodeFun DeptCardTree_NodeFun1 = new DeptCardTree_NodeFun();
            DeptCardTree_NodeFun1.getTreeView(tvmSub020000_02, 1);
            blnNotRepeat02 = true;
            blnNotRepeat02 = CheckDBObjectNotRepeat(DeptCardTree_NodeFun1.m_ALuser_car_group_detailed, 1);//mdel 0->door,1->card

            int sum = 0;
            if (!blnNotRepeat01)
            {
                sum += 1;
            }
            if (!blnNotRepeat02)
            {
                sum += 2;
            }
            if (sum>0)//資料重複
            {
                if (blnRunSQL)
                {
                    return sum;
                }
            }

            //--
            //add at 2017/10/13
            if (!blnRunSQL)
            {
                m_Sub020000ALData.Clear();
                m_Sub020000ALData.Add(txtSub020000_01.Text);
                m_Sub020000ALData.Add(ckbSub020000_10.Checked.ToString());
                m_Sub020000ALData.Add(ckbSub020000_11.Checked.ToString());
                m_Sub020000ALData.Add(ckbSub020000_12.Checked.ToString());
                m_Sub020000ALData.Add(ckbSub020000_13.Checked.ToString());
                m_Sub020000ALData.Add(ckbSub020000_14.Checked.ToString());
                m_Sub020000ALData.Add(ckbSub020000_15.Checked.ToString());
                m_Sub020000ALData.Add(ckbSub020000_16.Checked.ToString());
                m_Sub020000ALData.Add(ckbSub020000_17.Checked.ToString());
                m_Sub020000ALData.Add(ckbSub020000_18.Checked.ToString());
                m_Sub020000ALData.Add(ckbSub020000_19.Checked.ToString());
                m_Sub020000ALData.Add(ckbSub020000_20.Checked.ToString());
                m_Sub020000ALData.Add(ckbSub020000_21.Checked.ToString());
                m_Sub020000ALData.Add(ckbSub020000_22.Checked.ToString());
                m_Sub020000ALData.Add(ckbSub020000_23.Checked.ToString());
                m_Sub020000ALData.Add(ckbSub020000_24.Checked.ToString());
                m_Sub020000ALData.Add(ckbSub020000_25.Checked.ToString());
                m_Sub020000ALData.Add(ckbSub020000_26.Checked.ToString());
                m_Sub020000ALData.Add(rdbSub020000_01.Checked.ToString());
                m_Sub020000ALData.Add(rdbSub020000_02.Checked.ToString());
                m_Sub020000ALData.Add(rdbSub020000_03.Checked.ToString());
                m_Sub020000ALData.Add(rdbSub020000_04.Checked.ToString());
                m_Sub020000ALData.Add(cmbSub020000_02.SelectedIndex + "");
                m_Sub020000ALData.Add(adpSub020000_01.Value.ToString("yyyy-MM-dd HH:mm"));
                m_Sub020000ALData.Add(adpSub020000_02.Value.ToString("yyyy-MM-dd HH:mm"));
                m_Sub020000ALData.Add(steSub020000_01.StrValue1 + steSub020000_01.StrValue2);
                m_Sub020000ALData.Add(steSub020000_02.StrValue1 + steSub020000_02.StrValue2);
                m_Sub020000ALData.Add(steSub020000_03.StrValue1 + steSub020000_03.StrValue2);

                for (int i = 0; i < AreaTree_NodeFun1.m_ALarea_door_group_detailed.Count; i++)
                {
                    m_Sub020000ALData.Add(AreaTree_NodeFun1.m_ALarea_door_group_detailed[i].ToString());
                }

                for (int i = 0; i < DeptCardTree_NodeFun1.m_ALuser_car_group_detailed.Count; i++)
                {
                    m_Sub020000ALData.Add(DeptCardTree_NodeFun1.m_ALuser_car_group_detailed[i].ToString());
                }

                return 0;
            }
            //--

            if (m_intDB2LeftSub020000_id == -10)//新增
            {
                SQL = String.Format("INSERT INTO authorization_group (name, enable, card_type_enable, card_type, effective_date_enable, effective_date_start, effective_date_end, card_status_enable, card_disable, card_black, card_apb_disable, card_level_enable, card_level, card_week_time_enable, card_week, card_time_period01, card_time_period02, card_time_period03,action) VALUES ('{0}','{1}','{2}','{3}','{4}','{5}','{6}','{7}','{8}','{9}','{10}','{11}','{12}','{13}','{14}','{15}','{16}','{17}',1);",
                Strname, Strenable, Strcard_type_enable, Strcard_type, Streffective_date_enable, Streffective_date_start, Streffective_date_end, Strcard_status_enable, Strcard_disable, Strcard_black, Strcard_apb_disable, Strcard_level_enable, Strcard_level, Strcard_week_time_enable, Strcard_week, Strcard_time_period01, Strcard_time_period02, Strcard_time_period03);
            }
            else//修改
            {
                SQL = String.Format("UPDATE authorization_group SET name ='{0}', enable ='{1}', card_type_enable ='{2}', card_type ='{3}', effective_date_enable ='{4}', effective_date_start ='{5}', effective_date_end ='{6}', card_status_enable ='{7}', card_disable ='{8}', card_black ='{9}', card_apb_disable ='{10}', card_level_enable ='{11}', card_level ='{12}', card_week_time_enable ='{13}', card_week ='{14}', card_time_period01 ='{15}', card_time_period02 ='{16}', card_time_period03='{17}' WHERE id={18};",
                Strname, Strenable, Strcard_type_enable, Strcard_type, Streffective_date_enable, Streffective_date_start, Streffective_date_end, Strcard_status_enable, Strcard_disable, Strcard_black, Strcard_apb_disable, Strcard_level_enable, Strcard_level, Strcard_week_time_enable, Strcard_week, Strcard_time_period01, Strcard_time_period02, Strcard_time_period03, m_intDB2LeftSub020000_id);
            }
            MySQL.InsertUpdateDelete(SQL);

            if (m_intDB2LeftSub020000_id == -10)
            {
                SQL = String.Format("SELECT id FROM authorization_group WHERE name='{0}' ORDER BY id DESC;", Strname);
                MySqlDataReader DataReader = MySQL.GetDataReader(SQL);
                while (DataReader.Read())
                {
                    m_intDB2LeftSub020000_id = Convert.ToInt32(DataReader["id"].ToString());
                    break;
                }
                DataReader.Close();
            }

            SQL = String.Format("DELETE FROM authorization_group_detailed WHERE authorization_group_id={0};", m_intDB2LeftSub020000_id);
            MySQL.InsertUpdateDelete(SQL);

            if (DeptCardTree_NodeFun1.m_ALuser_car_group_detailed.Count > 0)
            {
                SQL = "";
                for (int i = 0; i < DeptCardTree_NodeFun1.m_ALuser_car_group_detailed.Count; i++)
                {
                    String StrData = DeptCardTree_NodeFun1.m_ALuser_car_group_detailed[i].ToString();
                    int type = -2;
                    if (StrData.IndexOf("-1") > 0)
                    {
                        type = -1;
                    }
                    SQL += String.Format("INSERT INTO authorization_group_detailed (authorization_group_id,data,data_type,state) VALUES ({0},'{1}',{2},{3});", m_intDB2LeftSub020000_id, StrData, type, state);
                    if (((i + 1) % 20 == 0))
                    {
                        MySQL.InsertUpdateDelete(SQL);
                        SQL = "";
                    }
                }
                if (SQL.Length > 0)
                {
                    MySQL.InsertUpdateDelete(SQL);
                }
            }

            if (AreaTree_NodeFun1.m_ALarea_door_group_detailed.Count > 0)
            {
                SQL = "";
                for (int i = 0; i < AreaTree_NodeFun1.m_ALarea_door_group_detailed.Count; i++)
                {
                    String StrData = AreaTree_NodeFun1.m_ALarea_door_group_detailed[i].ToString();
                    int type = 2;
                    if (StrData.IndexOf("-1") > 0)
                    {
                        type = 1;
                    }
                    SQL += String.Format("INSERT INTO authorization_group_detailed (authorization_group_id,data,data_type,state) VALUES ({0},'{1}',{2},{3});", m_intDB2LeftSub020000_id, StrData, type, state);
                    if (((i + 1) % 20 == 0))
                    {
                        MySQL.InsertUpdateDelete(SQL);
                        SQL = "";
                    }
                }
                if (SQL.Length > 0)
                {
                    MySQL.InsertUpdateDelete(SQL);
                }
            }
            //修改授權運算時間點-只有在執行批次處理『Applied』 authorization_data authorization_data=new authorization_data(m_intDB2LeftSub020000_id);// add 2017/11/16
            
            //---
            //呼叫顯示授權運算規則UI
            authorization_data authorization_data01 = new authorization_data(m_intDB2LeftSub020000_id);
            ShowAuthorizedParameterAns ShowAuthorizedParameterAns = new ShowAuthorizedParameterAns(authorization_data01);
            ShowAuthorizedParameterAns.ShowDialog();
            //---

            //---
            //設定顯示授權運算規則UI 按下OK表示直接建立 授權規則+授權運算，若按下Cancel表示單純建立授權規則
            if (authorization_data.m_intRun > -1)
            {
                //authorization_data01.calculate_saveDB();//修正運算授權結果用Filter方法-運算函數修正
                //authorization_data.sendData2HW();//整合呼叫傳送授權API的函數
                Animation.m_ADThreadBuf = authorization_data01;
                Animation.createThreadAnimation(Language.m_StrAuthorizeStepMsg01, Animation.Thread_authorization);
                MessageBox.Show(Language.m_StrAuthTransferMsg02, Language.m_StrAuthTransferMsg01, MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
            //---

            return 0;
        }

        private void butSub020000_04_Click(object sender, EventArgs e)//新增 群組
        {
            if (txtSub020000_01.Text.Length > 0)
            {
                labSub020000_02.ForeColor = Color.Black;

                int Ans = AuthorizationGroupUI2DB();
                if (Ans == 0)
                {
                    initdgvSub020000_01();

                    initLeftSub020000UI();
                    m_intDB2LeftSub020000_id = -1;
                    LeftSub020000UImode();

                    initdgvSub0200_01();
                    Leave_function();
                }
                else
                {
                    switch (Ans)
                    {
                        case 1:
                            MessageBox.Show(Language.m_StrAuthorizationMsg01, Language.m_StrAuthorizationMsg00, MessageBoxButtons.OK, MessageBoxIcon.Error);
                            break;
                        case 2:
                            MessageBox.Show(Language.m_StrAuthorizationMsg02, Language.m_StrAuthorizationMsg00, MessageBoxButtons.OK, MessageBoxIcon.Error);
                            break;
                        case 3:
                            MessageBox.Show(Language.m_StrAuthorizationMsg03, Language.m_StrAuthorizationMsg00, MessageBoxButtons.OK, MessageBoxIcon.Error);
                            break;
                    }
                }
            }
            else
            {
                labSub020000_02.ForeColor = Color.Red;
                MessageBox.Show(Language.m_StrbutSub020000_04Msg01, butSub020000_04.Text, MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void butSub020000_03_Click(object sender, EventArgs e)//編輯 群組
        {
            if (txtSub020000_01.Text.Length > 0)
            {
                labSub020000_02.ForeColor = Color.Black;

                int Ans = AuthorizationGroupUI2DB();
                if (Ans == 0)
                {
                    initdgvSub020000_01();

                    initLeftSub020000UI();
                    m_intDB2LeftSub020000_id = -1;
                    LeftSub020000UImode();

                    initdgvSub0200_01();
                    Leave_function();
                }
                else
                {
                    switch (Ans)
                    {
                        case 1:
                            MessageBox.Show(Language.m_StrAuthorizationMsg01, Language.m_StrAuthorizationMsg00, MessageBoxButtons.OK, MessageBoxIcon.Error);
                            break;
                        case 2:
                            MessageBox.Show(Language.m_StrAuthorizationMsg02, Language.m_StrAuthorizationMsg00, MessageBoxButtons.OK, MessageBoxIcon.Error);
                            break;
                        case 3:
                            MessageBox.Show(Language.m_StrAuthorizationMsg03, Language.m_StrAuthorizationMsg00, MessageBoxButtons.OK, MessageBoxIcon.Error);
                            break;
                    }
                    
                }
            }
            else
            {
                labSub020000_02.ForeColor = Color.Red;
                MessageBox.Show(Language.m_StrbutSub020000_03Msg01, butSub020000_03.Text, MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void dgvSub020000_01_DoubleClick(object sender, EventArgs e)//授權列表-雙點擊呼叫編輯
        {
            butSub020000_15.PerformClick();
        }

        private void picSub020000_02_Click(object sender, EventArgs e)//縮小授權參數區域
        {
            //--
            //add 2017/10/20
            picSub020000_01.Visible = true;

            bgpSub020000_03.Height = 56;
            
            bgpSub020000_04.Location = new Point(6, 487 - (386-56));
            bgpSub020000_05.Location = new Point(402, 487 - (386 - 56));
            bgpSub020000_04.Height = 399 + (386 - 56);
            bgpSub020000_05.Height = 399 + (386 - 56);
            //--
        }

        private void picSub020000_01_Click(object sender, EventArgs e)//展開授權參數區域
        {
            //--
            //add 2017/10/20
            picSub020000_01.Visible = false;

            bgpSub020000_03.Height = 386;
            
            bgpSub020000_04.Location = new Point(6, 487);
            bgpSub020000_05.Location = new Point(402, 487);
            bgpSub020000_04.Height = 399;
            bgpSub020000_05.Height = 399;
            //--
        }
        //Sub020000_end
        
        //Sub0201_start
        //Sub0201_end
        
        //Sub0202_start

        //Sub0202_end
        //Sub0203_start
        //---
        //製作多選查詢授權紀錄-左右鍵切換觀看指定授權內容 ~ 撰寫儲存查詢授權紀錄變數相關
        public ArrayList m_ALAuthObj = new ArrayList();
        public int m_intAuthIndex;
        private void initSelectAuthArray(int select)
        {
            m_ALAuthObj.Clear();
            m_intAuthIndex = 0;

            switch(select)
            {
                case 1:
                    for (int i = 0; i < tvmSub0203_01.m_coll.Count; i++)
                    {
                        Tree_Node buf = (Tree_Node)(tvmSub0203_01.m_coll[i]);
                        if (buf.m_data != "")
                        {
                            m_ALAuthObj.Add(buf);
                        }
                    }
                    if (m_ALAuthObj.Count > 1)
                    {
                        TN_tvmSub0203_01 = (Tree_Node)m_ALAuthObj[0];
                        butSub020300_02.Visible = true;
                        butSub020300_03.Visible = true;
                    }
                    else
                    {
                        butSub020300_02.Visible = false;
                        butSub020300_03.Visible = false;
                    }
                    break;
                case 2:
                    for (int i = 0; i < tvmSub0203_02.m_coll.Count; i++)
                    {
                        Tree_Node buf = (Tree_Node)(tvmSub0203_02.m_coll[i]);
                        if (buf.m_data != "")
                        {
                            m_ALAuthObj.Add(buf);
                        }
                    }
                    if (m_ALAuthObj.Count > 1)
                    {
                        TN_tvmSub0203_02 = (Tree_Node)m_ALAuthObj[0];
                        butSub020300_02.Visible = true;
                        butSub020300_03.Visible = true;
                    }
                    else
                    {
                        butSub020300_02.Visible = false;
                        butSub020300_03.Visible = false;
                    }
                    break;
            }
        }
        //---製作多選查詢授權紀錄-左右鍵切換觀看指定授權內容 ~ 撰寫儲存查詢授權紀錄變數相關

        private void butSub0203_02_Click(object sender, EventArgs e)
        {
            initSelectAuthArray(1);//製作多選查詢授權紀錄-左右鍵切換觀看指定授權內容 ~ 撰寫儲存查詢授權紀錄變數相關

            if (TN_tvmSub0203_01 != null)//門區授權查詢要支援滑鼠雙點擊功能 if (tvmSub0203_01.SelectedNode != null)
            {
                Tree_Node tmp = TN_tvmSub0203_01;//門區授權查詢要支援滑鼠雙點擊功能 Tree_Node tmp = ((Tree_Node)tvmSub0203_01.SelectedNode);
                if (tmp.m_tree_level < 0)
                {
                    //MessageBox.Show(tmp.Text + ",\n資料=" + tmp.m_data + ",\nID=" + tmp.m_id + ",\n階層=" + tmp.m_tree_level + ",\n父編=" + tmp.m_unit);

                    m_TPOld = m_tabMain.SelectedTab;//--2017/02/22 製作返回按鈕功能
                    TabPage_Push();//m_StackTPOld.Push(m_TPOld);//--2017/02/22 製作返回按鈕功能

                    m_tabSub020300.Parent = m_tabMain;
                    //----------------------
                    CleanSub020300UIVar();
                    SetSub020300UIVar(1, tmp.Text, tmp.m_id);
                    initSub020300UI();
                    //----------------------
                    m_tabMain.SelectedTab = m_tabSub020300;
                }
                else
                {
                }
            }
            tvmSub0203_01.SelectedNode = null;
        }

        private void butSub0203_03_Click(object sender, EventArgs e)
        {
            initSelectAuthArray(2);//製作多選查詢授權紀錄-左右鍵切換觀看指定授權內容 ~ 撰寫儲存查詢授權紀錄變數相關

            if (TN_tvmSub0203_02 != null)//門區授權查詢要支援滑鼠雙點擊功能 if (tvmSub0203_02.SelectedNode != null)
            {
                Tree_Node tmp = TN_tvmSub0203_02;//門區授權查詢要支援滑鼠雙點擊功能 Tree_Node tmp = ((Tree_Node)tvmSub0203_02.SelectedNode);
                if (tmp.m_tree_level < 0)
                {
                    //MessageBox.Show(tmp.Text + ",\n資料=" + tmp.m_data + ",\nID=" + tmp.m_id + ",\n階層=" + tmp.m_tree_level + ",\n父編=" + tmp.m_unit);

                    m_TPOld = m_tabMain.SelectedTab;//--2017/02/22 製作返回按鈕功能
                    TabPage_Push();//m_StackTPOld.Push(m_TPOld);//--2017/02/22 製作返回按鈕功能

                    m_tabSub020300.Parent = m_tabMain;
                    //----------------------
                    CleanSub020300UIVar();
                    SetSub020300UIVar(0, tmp.Text, tmp.m_id);
                    initSub020300UI();
                    //----------------------
                    m_tabMain.SelectedTab = m_tabSub020300;
                }
                else
                {
                }
            }
            tvmSub0203_02.SelectedNode = null;
        }

        private void tvmSub0203_01_AfterSelect(object sender, TreeViewEventArgs e)//修正授權紀錄查詢UI的元件展開沒有動畫效果問題
        {
            Tree_Node tmp = ((Tree_Node)tvmSub0203_01.SelectedNode);
            TN_tvmSub0203_01 = tmp;//門區授權查詢要支援滑鼠雙點擊功能
            tmp.Expand();
        }

        private void tvmSub0203_02_AfterSelect(object sender, TreeViewEventArgs e)//修正授權紀錄查詢UI的元件展開沒有動畫效果問題
        {
            Tree_Node tmp = ((Tree_Node)tvmSub0203_02.SelectedNode);
            TN_tvmSub0203_02 = tmp;//門區授權查詢要支援滑鼠雙點擊功能
            tmp.Expand();
        }

        private void tvmSub0203_01_AfterExpand(object sender, TreeViewEventArgs e)
        {
            
        }

        private void tvmSub0203_02_AfterExpand(object sender, TreeViewEventArgs e)
        {

        }

        //Sub0203_end
        //Sub0300_start
        private void ckbSub0300_Changed(object sender, EventArgs e)
        {

            //---
            //設計開發報表UI-撰寫基本事件
            cmbSub0300_01.Enabled = ckbSub0300_01.Checked;
            cmbSub0300_02.Enabled = ckbSub0300_02.Checked;
            cmbSub0300_03.Enabled = ckbSub0300_03.Checked;
            txtSub0300_01.Enabled = ckbSub0300_03.Checked;
            cmbSub0300_04.Enabled = ckbSub0300_04.Checked;
            dtpSub0300_01.Enabled = ckbSub0300_05.Checked;
            dtpSub0300_02.Enabled = ckbSub0300_05.Checked;
            //---

            //---
            //設計開發報表UI-紀錄過濾器開啟狀態
            int intstate = 0;
            if(ckbSub0300_01.Checked)
            {
                intstate += 4;
            }
            else
            {
                intstate += 0;
                cmbSub0300_01.SelectedIndex = -1;
            }
            if(ckbSub0300_02.Checked)
            {
                intstate += 2;
            }
            else
            {
                intstate += 0;
                cmbSub0300_02.SelectedIndex = -1;
            }
            if(ckbSub0300_03.Checked)
            {
                intstate += 1;
            }
            else
            {
                intstate += 0;
                cmbSub0300_03.SelectedIndex = -1;
                txtSub0300_01.Text = "";
            }
            if(ckbSub0300_04.Checked)
            {
                intstate += 16;
            }
            else
            {
                intstate += 0;
                cmbSub0300_04.SelectedIndex = -1;
            }
            if(ckbSub0300_05.Checked)
            {
                intstate += 32;
            }
            else
            {
                intstate += 0;
            }
            //---

            //---
            //設計開發報表UI-卡號元件自動切換
            if (((intstate & 2) == 2) || ((intstate & 4) == 4))
            {
                txtSub0300_01.Text = "";
                txtSub0300_01.Visible = false;
                cmbSub0300_03.Visible = true;
            }
            else
            {
                txtSub0300_01.Visible = true;
                cmbSub0300_03.Visible = false;
            }
            //---
        }

        private void cmbSub0300_01_SelectedIndexChanged(object sender, EventArgs e)//設計開發報表UI-報表部門選擇事件
        {
            if (cmbSub0300_01.SelectedIndex > -1)
            {
                SetcmbSub0300_02(true, Convert.ToInt32(m_ALDepartment_ID[cmbSub0300_01.SelectedIndex].ToString()));
                SetcmbSub0300_03(1, Convert.ToInt32(m_ALDepartment_ID[cmbSub0300_01.SelectedIndex].ToString()));
            }
            else
            {
                SetcmbSub0300_02();
                SetcmbSub0300_03();
            }
        }

        private void cmbSub0300_02_SelectedIndexChanged(object sender, EventArgs e)//設計開發報表UI-報表人/車選擇事件
        {
            if (cmbSub0300_02.SelectedIndex > -1)
            {
                SetcmbSub0300_03(2, Convert.ToInt32(m_ALUserCar_ID[cmbSub0300_02.SelectedIndex].ToString()), Convert.ToInt32(m_ALUserCar_State[cmbSub0300_02.SelectedIndex].ToString()));
            }
            else
            {
                SetcmbSub0300_03();
            }
        }

        private void butSub0300_01_Click(object sender, EventArgs e)//設計開發報表UI-執行產生報表
        {
            int state = 0;
            String SQL="";
            //---
            //報表UI SQL優化
            //String MainSQL = "SELECT u.name AS user_name,c.name AS car_name,cd.display AS card_name,cir.card_unique_identifier AS card_code,rs.name AS state,cir.timestamp AS time FROM controller_io_record AS cir LEFT JOIN card AS cd ON cir.card_unique_identifier = cd.card_code LEFT JOIN record_status AS rs ON cir.status = rs.id LEFT JOIN card_for_user_car AS cfuc ON cfuc.card_id = cd.id LEFT JOIN user AS u ON u.id = cfuc.user_id LEFT JOIN car AS c ON c.id = car_id";
            String MainSQL = "SELECT u.name AS UCname,cd.display AS card_name,cir.card_unique_identifier AS card_code,rs.name AS state,cir.timestamp AS time FROM controller_io_record AS cir,card AS cd,record_status AS rs,card_for_user_car AS cfuc,user AS u WHERE (cir.card_unique_identifier = cd.card_code) AND (cir.status = rs.id) AND (cfuc.card_id = cd.id ) AND (u.id = cfuc.user_id) UNION ALL SELECT c.name AS UCname,cd.display AS card_name,cir.card_unique_identifier AS card_code,rs.name AS state,cir.timestamp AS time FROM controller_io_record AS cir,card AS cd,record_status AS rs,card_for_user_car AS cfuc,car AS c WHERE (cir.card_unique_identifier = cd.card_code) AND (cir.status = rs.id) AND (cfuc.card_id = cd.id ) AND (c.id = car_id);";
            //---報表UI SQL優化

            String SubSQL="";
            if ((txtSub0300_01.Text != "")||(cmbSub0300_03.SelectedIndex!=-1))//有選卡片-就不用管 [人/車] 和 [部門] 選項
            {
                    if (txtSub0300_01.Text != "")
                    {
                        String Strcard_code = txtSub0300_01.Text.PadLeft(16, '0').ToUpper();//補齊16個字且全部轉大寫
                        txtSub0300_01.Text = Strcard_code;
                        //---
                        //報表UI SQL優化
                        //SubSQL = String.Format(" WHERE cir.card_unique_identifier = '{0}'", Strcard_code);
                        SubSQL = String.Format(" (cir.card_unique_identifier = '{0}')", Strcard_code);
                        //---報表UI SQL優化
                    }
                    else
                    {
                        //---
                        //報表UI SQL優化
                        //SubSQL = String.Format(" WHERE cir.card_unique_identifier = '{0}'", m_ALCard_Code[cmbSub0300_03.SelectedIndex].ToString());
                        SubSQL = String.Format(" (cir.card_unique_identifier = '{0}')", m_ALCard_Code[cmbSub0300_03.SelectedIndex].ToString());
                        //---報表UI SQL優化
                    }
            }
            else//搜尋非單一卡
            {
                if (cmbSub0300_02.SelectedIndex != -1)//有選人/車-就不用管部門
                {
                    if (m_ALUserCar_State[cmbSub0300_02.SelectedIndex] == "0")
                    {
                        //---
                        //報表UI SQL優化
                        //SubSQL = String.Format(" WHERE cir.card_unique_identifier IN (SELECT cd.card_code AS card_code FROM card_for_user_car AS cfuc,card AS cd WHERE (cd.id=cfuc.card_id) AND (cfuc.user_id = '{0}') )", m_ALUserCar_ID[cmbSub0300_02.SelectedIndex].ToString());
                        SubSQL = String.Format(" (cir.card_unique_identifier IN (SELECT cd.card_code AS card_code FROM card_for_user_car AS cfuc,card AS cd WHERE (cd.id=cfuc.card_id) AND (cfuc.user_id = '{0}') ) )", m_ALUserCar_ID[cmbSub0300_02.SelectedIndex].ToString());
                        //---報表UI SQL優化
                    }
                    else
                    {
                        //---
                        //報表UI SQL優化
                        //SubSQL = String.Format(" WHERE cir.card_unique_identifier IN (SELECT cd.card_code AS card_code FROM card_for_user_car AS cfuc,card AS cd WHERE (cd.id=cfuc.card_id) AND (cfuc.car_id = '{0}') )", m_ALUserCar_ID[cmbSub0300_02.SelectedIndex].ToString());
                        SubSQL = String.Format(" (cir.card_unique_identifier IN (SELECT cd.card_code AS card_code FROM card_for_user_car AS cfuc,card AS cd WHERE (cd.id=cfuc.card_id) AND (cfuc.car_id = '{0}') ) )", m_ALUserCar_ID[cmbSub0300_02.SelectedIndex].ToString());
                        //---報表UI SQL優化
                    }
                }
                else
                {
                    if (cmbSub0300_01.SelectedIndex != -1)//選部門
                    {
                        //---
                        //報表UI SQL優化
                        //SubSQL = String.Format(" WHERE cir.card_unique_identifier IN (SELECT cd.card_code AS card_code FROM card AS cd,department_detail AS dd,card_for_user_car AS cfuc WHERE (cfuc.card_id=cd.id) AND ((dd.user_id=cfuc.user_id) OR (dd.car_id=cfuc.car_id)) AND (dd.dep_id='{0}'))", m_ALDepartment_ID[cmbSub0300_01.SelectedIndex].ToString());
                        SubSQL = String.Format(" (cir.card_unique_identifier IN (SELECT cd.card_code AS card_code FROM card AS cd,department_detail AS dd,card_for_user_car AS cfuc WHERE (cfuc.card_id=cd.id) AND ((dd.user_id=cfuc.user_id) OR (dd.car_id=cfuc.car_id)) AND (dd.dep_id='{0}')))", m_ALDepartment_ID[cmbSub0300_01.SelectedIndex].ToString());
                        //---報表UI SQL優化
                    }
                    else
                    {
                    SubSQL = "";
                    }
                }                
            }

            String Filter = "";
            if (ckbSub0300_04.Checked && cmbSub0300_04.SelectedIndex > -1)
            {
                Filter += String.Format("cir.status = '{0}'", m_ALRecordStatus_ID[cmbSub0300_04.SelectedIndex].ToString());
            }

            if (ckbSub0300_05.Checked)
            {
                if (Filter.Length > 0)
                {
                    Filter += " AND ";
                }
                DateTime dt_base = new DateTime(2000, 01, 01, 00, 00, 00);
                //---
                //調整報表元件結束時間為23:59
                DateTime dt_start = new DateTime(dtpSub0300_01.Value.Year,dtpSub0300_01.Value.Month,dtpSub0300_01.Value.Day, 00, 00, 00);
                DateTime dt_end = new DateTime(dtpSub0300_02.Value.Year, dtpSub0300_02.Value.Month, dtpSub0300_02.Value.Day, 23, 59, 59);
                //---調整報表元件結束時間為23:59
                TimeSpan ts_start = dt_start - dt_base;
                TimeSpan ts_end = dt_end - dt_base;
                int intstart = (int)(ts_start.TotalSeconds);
                int intend = (int)(ts_end.TotalSeconds);
                Filter += "(" + intstart + "<= cir.timestamp AND cir.timestamp<=" + intend + ")";
            }

            if (Filter.Length > 0)
            {
                //---
                //報表UI SQL優化
                /*
                if (SubSQL.Length > 0)
                {
                    SubSQL += " AND " + Filter;
                }
                else
                {
                    SubSQL = " WHERE " + Filter;
                }
                */
                if (SubSQL.Length > 0)
                {
                    SubSQL += " AND " + Filter;
                }
                else
                {
                    SubSQL = Filter;
                }
                //---報表UI SQL優化
            }

            //---
            //報表UI SQL優化
            //SQL = MainSQL + SubSQL + " ORDER BY cir.card_unique_identifier,cir.timestamp;";//修正SQL語法預設用卡號+時間排序 ORDER BY cir.card_unique_identifier,cir.timestamp
            if (SubSQL.Length > 0)
            {
                SQL = String.Format("SELECT u.name AS UCname,cd.display AS card_name,cir.card_unique_identifier AS card_code,rs.name AS state,cir.timestamp AS time FROM controller_io_record AS cir,card AS cd,record_status AS rs,card_for_user_car AS cfuc,user AS u WHERE (cir.card_unique_identifier = cd.card_code) AND (cir.status = rs.id) AND (cfuc.card_id = cd.id ) AND (u.id = cfuc.user_id) AND {0} UNION ALL SELECT c.name AS UCname,cd.display AS card_name,cir.card_unique_identifier AS card_code,rs.name AS state,cir.timestamp AS time FROM controller_io_record AS cir,card AS cd,record_status AS rs,card_for_user_car AS cfuc,car AS c WHERE (cir.card_unique_identifier = cd.card_code) AND (cir.status = rs.id) AND (cfuc.card_id = cd.id ) AND (c.id = car_id) AND {0};", SubSQL);
            }
            else
            {
                SQL = MainSQL;
            }
            //---報表UI SQL優化

            SetrptSub0300(0, SQL);
        }

        private void butSub0300_02_Click(object sender, EventArgs e)
        {
            //---
            //下載指定時間報表資料
            bool blnAns = true;
            blnAns = m_ExMySQL.CheckMySQL(txtSys_01.Text, txtSys_07.Text, txtSys_02.Text, txtSys_03.Text);//增加外部SERVER測試函數
            if (blnAns == true)
            {
                DateTime dt_base = new DateTime(2000, 01, 01, 00, 00, 00);

                if (ckbSub0300_06.Checked)
                {
                    //---
                    //調整報表元件結束時間為23:59
                    DateTime dt_start = new DateTime(dtpSub0300_03.Value.Year, dtpSub0300_03.Value.Month, dtpSub0300_03.Value.Day, 00, 00, 00);
                    DateTime dt_end = new DateTime(dtpSub0300_04.Value.Year, dtpSub0300_04.Value.Month, dtpSub0300_04.Value.Day, 23, 59, 59);
                    dt_end.AddMinutes(23 * 60 + 59);
                    //---調整報表元件結束時間為23:59
                    TimeSpan ts_start = dt_start - dt_base;
                    TimeSpan ts_end = dt_end - dt_base;
                    int intstart = (int)(ts_start.TotalSeconds);
                    int intend = (int)(ts_end.TotalSeconds);
                    m_StrDumpWherecondition = "(" + intstart + "<= timestamp AND timestamp<=" + intend + ")";
                }
                else
                {
                    int intstart = 0;
                    TimeSpan ts_end = DateTime.Now - dt_base;
                    int intend = (int)(ts_end.TotalSeconds);
                    m_StrDumpWherecondition = "(" + intstart + "<= timestamp AND timestamp<=" + intend + ")";
                }

                Animation.createThreadAnimation(butSub0300_02.Text, Animation.Thread_DownloadIORecord);//重寫匯入控制器按鈕變成有等待動畫程式

                MessageBox.Show(Language.m_StrSysMsg01, butSub0300_02.Text, MessageBoxButtons.OK, MessageBoxIcon.Information);

                SetrptSub0300();
            }
            else
            {
                MessageBox.Show(Language.m_StrConnectMsg02, Language.m_StrConnectMsg00, MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            //---下載指定時間報表資料
        }

        private void txtSub0300_01_KeyUp(object sender, KeyEventArgs e)//設計開發報表UI-卡片內碼元件要能支援C/P
        {
            //https://dotblogs.com.tw/chou/2011/12/20/62709
            //http://www.cnblogs.com/han1982/p/4770270.html
            //https://fredxxx123.wordpress.com/2008/11/22/c-%E8%A4%87%E8%A3%BD%E8%B3%87%E6%96%99%E5%88%B0%E5%89%AA%E8%B2%BC%E7%B0%BF/
            //https://social.msdn.microsoft.com/Forums/zh-TW/3cc1d2be-5be7-4388-831e-2b5485b3b509/-textbox-?forum=233
            if (e.KeyData == (Keys.Control | Keys.A))
            {
                txtSub0300_01.SelectAll();
            }
            if (e.KeyData == (Keys.Control | Keys.C))
            {
                //MessageBox.Show("Ctrl + C");
                Clipboard.SetData(DataFormats.Text, txtSub0300_01.Text);
            }
            if (e.KeyData == (Keys.Control | Keys.V))//偵測Ctrl+v
            {
                //MessageBox.Show("Ctrl + V");
                if (Clipboard.ContainsText())
                {
                    try
                    {
                        Convert.ToInt32(Clipboard.GetText());  //检查是否数字
                        ((TextBox)sender).SelectedText = Clipboard.GetText().Trim(); //Ctrl+V 粘贴  
                        if (((TextBox)sender).TextLength > 16)
                        {
                            ((TextBox)sender).Text = ((TextBox)sender).Text.Remove(16); //TextBox最大长度为16  移除多余的
                        }
                    }
                    catch (Exception)
                    {
                        e.Handled = true;
                        //throw;
                    }
                }
            }
        }

        private void txtSub0300_01_KeyPress(object sender, KeyPressEventArgs e)//設計開發報表UI-卡片內碼防呆限制
        {
            if (e.KeyChar == 8)//刪除鍵要直接允許
            {
                e.Handled = false;
            }
            else
            {
                if (txtSub0300_01.Text.Length < 16)//長度限制在16
                {
                    if ((e.KeyChar >= 'a' && e.KeyChar <= 'f') || (e.KeyChar >= 'A' && e.KeyChar <= 'F') || (e.KeyChar >= '0' && e.KeyChar <= '9'))//限制0~9和A~F
                    {
                        e.Handled = false;
                    }
                    else
                    {
                        e.Handled = true;
                    }
                }
                else
                {
                    e.Handled = true;
                }
            }
        }

        //---
        //把下載報表資訊的指定時間元件切開
        private void ckbSub0300_06_CheckedChanged(object sender, EventArgs e)
        {
            dtpSub0300_03.Enabled = ckbSub0300_06.Checked;
            dtpSub0300_04.Enabled = ckbSub0300_06.Checked;
        }
        //---把下載報表資訊的指定時間元件切開

        //Sub0300_end
        //Sub0004_start
        private void dgvSub0004_01_DoubleClick(object sender, EventArgs e)//列表支援雙點擊
        {
            butSub0004_01.PerformClick();
        }

        private void butSub0004_03_Click(object sender, EventArgs e)//製作SYDM列表UI-製作SYDM匯入功能
        {
            Animation.createThreadAnimation(butSub0004_03.Text, Animation.Thread_getSYDMList);//SYDM匯入變成有等待動畫
            if(Animation.m_blnAns)//if (HW_Net_API.SYCG_getSYDMList())
            {
                String SQL = "";
                for (int i = 0; i < m_Sydms.sydms.Count; i++)
                {
                    String Name = String.Format("SYDM_{0:0000}", i);
                    String IP = String.Format("{0}:{1}", HW_Net_API.long2ip(m_Sydms.sydms[i].connection.address, true), m_Sydms.sydms[i].connection.port); //修正所有API內有關IP的運算公式變成32位元版-允許負數 //把IP轉換函數從32位元版改回64位元版-不允許有負數
                    String active = "" + m_Sydms.sydms[i].active;
                    String Identifier = "" + m_Sydms.sydms[i].identifier;
                    String Api_Key = m_Sydms.sydms[i].setup.api_key;
                    
                    SQL = String.Format("SELECT id FROM sydm WHERE ip_address='{0}'",IP);
                    MySqlDataReader Reader_Data = MySQL.GetDataReader(SQL);
                    bool blnHas = Reader_Data.HasRows;
                    Reader_Data.Close();
                    
                    if (!blnHas)
                    {
                        SQL = String.Format("INSERT INTO sydm (name, identifier, ip_address ,active ,api_key) VALUES ('{0}','{1}','{2}','{3}','{4}');", Name, Identifier, IP, active, Api_Key);
                        MySQL.InsertUpdateDelete(SQL);
                    }
                }
                initdgvSub0004_01();
            }
            else//呼叫API失敗
            {
                MessageBox.Show(Language.m_StrConnectMsg02, Language.m_StrConnectMsg00, MessageBoxButtons.OK, MessageBoxIcon.Error);
            }

        }

        private void butSub0004_02_Click(object sender, EventArgs e)//製作SYDM列表UI-新增SYDM功能
        {
            m_TPOld = m_tabMain.SelectedTab;//--2017/02/22 製作返回按鈕功能
            TabPage_Push();//m_StackTPOld.Push(m_TPOld);//--2017/02/22 製作返回按鈕功能

            //m_intdgvSub0104_01_id = -10;
            initSub000400UI();

            m_Sub000400ALInit.Clear();
            m_Sub000400ALInit.Add(m_intSub000400id.ToString());
            m_Sub000400ALInit.Add(txtSub000400_01.Text);
            m_Sub000400ALInit.Add(txtSub000400_02.Text);
            m_Sub000400ALInit.Add(txtSub000400_03.Text);
            m_Sub000400ALInit.Add(txtSub000400_04.Text);
            //---
            //SYDM要能停用
            if (ckbSub000400_01.Checked)
            {
                m_Sub000400ALInit.Add("1");
            }
            else
            {
                m_Sub000400ALInit.Add("0");
            }
            //---SYDM要能停用

            m_tabSub000400.Parent = m_tabMain;
            m_tabMain.SelectedTab = m_tabSub000400;
        }

        private void butSub0004_01_Click(object sender, EventArgs e)//製作SYDM列表UI-編輯SYDM功能
        {
            m_TPOld = m_tabMain.SelectedTab;//--2017/02/22 製作返回按鈕功能
            TabPage_Push();//m_StackTPOld.Push(m_TPOld);//--2017/02/22 製作返回按鈕功能

            //m_intdgvSub0104_01_id = -10;
            initSub000400UI();
            try
            {
                int index = dgvSub0004_01.SelectedRows[0].Index;//取得被選取的第一列位置
                String Strid = dgvSub0004_01.Rows[index].Cells[1].Value.ToString();
                m_intSub000400id = Int32.Parse(Strid);

                String SQL = String.Format("SELECT id,name,ip_address,identifier,active FROM sydm WHERE id={0}",m_intSub000400id);
                MySqlDataReader DataReader = MySQL.GetDataReader(SQL);
                while (DataReader.Read())
                {
                    String StrID = DataReader["id"].ToString();
                    txtSub000400_01.Text = DataReader["name"].ToString();

                    String StrIP = DataReader["ip_address"].ToString();
                    string[] strs = StrIP.Split(':');
                    txtSub000400_02.Text = strs[0];
                    txtSub000400_03.Text = strs[1];

                    String Strsydm_id = DataReader["identifier"].ToString();

                    //---
                    //SYDM要能停用
                    if (DataReader["active"].ToString() == "1")
                    {
                        ckbSub000400_01.Checked = true;
                    }
                    else
                    {
                        ckbSub000400_01.Checked = false;
                    }
                    //---SYDM要能停用

                    txtSub000400_04.Text = "ABCD";
                }
                DataReader.Close();

                m_Sub000400ALInit.Clear();
                m_Sub000400ALInit.Add(m_intSub000400id.ToString());
                m_Sub000400ALInit.Add(txtSub000400_01.Text);
                m_Sub000400ALInit.Add(txtSub000400_02.Text);
                m_Sub000400ALInit.Add(txtSub000400_03.Text);
                m_Sub000400ALInit.Add(txtSub000400_04.Text);
                //---
                //SYDM要能停用
                if (ckbSub000400_01.Checked)
                {
                    m_Sub000400ALInit.Add("1");
                }
                else
                {
                    m_Sub000400ALInit.Add("0");
                }
                //---SYDM要能停用

                m_tabSub000400.Parent = m_tabMain;
                m_tabMain.SelectedTab = m_tabSub000400;
            }
            catch
            {
            }
        }

        private void dgvSub0004_01_SelectionChanged(object sender, EventArgs e)
        {
            //---
            //列表元件shift全選時最前面的狀態要能自動打勾，反之要能取消
            for (int i = 0; i < dgvSub0004_01.Rows.Count; i++)
            {
                dgvSub0004_01.Rows[i].Cells[0].Value = false;
            }
            for (int j = 0; j < dgvSub0004_01.SelectedRows.Count; j++)
            {
                dgvSub0004_01.SelectedRows[j].Cells[0].Value = true;
            }
            //---列表元件shift全選時最前面的狀態要能自動打勾，反之要能取消
        }
        //Sub0004_end
        //Sub000400_start
        private void txtSub000400_03_KeyUp(object sender, KeyEventArgs e)//PORT的防呆~只能輸入1~65535
        {
            int temp = 0;
            try
            {
                temp = Convert.ToInt32(txtSub000400_03.Text);
            }
            catch
            {
                temp = 24410;
            }
            if (!(temp >= 1 && temp <= 65535))
            {
                temp = 24410;
            }
            txtSub000400_03.Text = "" + temp;
        }

        private void txtSub000400_03_KeyPress(object sender, KeyPressEventArgs e)//PORT的防呆~只能輸入數字
        {
            if (e.KeyChar == 8)//刪除鍵要直接允許
            {
                e.Handled = false;
            }
            else
            {
                if (e.KeyChar >= '0' && e.KeyChar <= '9')//限制0~9
                {
                    e.Handled = false;
                }
                else
                {
                    e.Handled = true;
                }
            }
        }

        private void butSub000400_00_Click(object sender, EventArgs e)//離開
        {
            m_Sub000400ALData.Clear();
            m_Sub000400ALData.Add(m_intSub000400id.ToString());
            m_Sub000400ALData.Add(txtSub000400_01.Text);
            m_Sub000400ALData.Add(txtSub000400_02.Text);
            m_Sub000400ALData.Add(txtSub000400_03.Text);
            m_Sub000400ALData.Add(txtSub000400_04.Text);
            //---
            //SYDM要能停用
            if (ckbSub000400_01.Checked)
            {
                m_Sub000400ALData.Add("1");
            }
            else
            {
                m_Sub000400ALData.Add("0");
            }
            //---SYDM要能停用
            if (CheckUIVarNotChange(m_Sub000400ALInit, m_Sub000400ALData))
            {
                initdgvSub0004_01();
                Leave_function();
            }
            else
            {
                DialogResult myResult = MessageBox.Show(Language.m_StrControllerMsg00, butSub0004_02.Text.Trim() + "/" + butSub0004_01.Text.Trim(), MessageBoxButtons.YesNo, MessageBoxIcon.Question);
                if (myResult == DialogResult.Yes)
                {
                    initdgvSub0004_01();
                    Leave_function();
                }
            }
        }

        private void butSub000400_01_Click(object sender, EventArgs e)// 新增/編修SYDM
        {
            String SQL = "";
            String Name = txtSub000400_01.Text.Trim();
            String IP = String.Format("{0}:{1}", txtSub000400_02.Text, txtSub000400_03.Text);
            String active = "1";
            String Identifier = "0";
            String Api_Key = txtSub000400_04.Text;

            //---
            //SYDM要能停用
            if (ckbSub000400_01.Checked)
            {
                active = "1";
            }
            else
            {
                active = "0";
            }
            //---SYDM要能停用
            
            if (Name.Length == 0)
            {
                labSub000400_01.ForeColor = Color.Red;
                MessageBox.Show(Language.m_StrbutSub000400_01Msg00, butSub000400_01.Text.Trim(), MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }
            else
            {
                labSub000400_01.ForeColor = Color.Black;
            }
            SQL = String.Format("SELECT id FROM sydm WHERE ip_address='{0}'", IP);
            MySqlDataReader Reader_Data = MySQL.GetDataReader(SQL);
            bool blnHas = Reader_Data.HasRows;
            Reader_Data.Close();

            if (m_intSub000400id==-10)
            {
                if (!blnHas)
                {
                    addSydm AddSydm = new addSydm();
                    AddSydm.active = Convert.ToInt32(active);
                    AddSydm.setup.api_key = Api_Key;
                    AddSydm.connection.port = Convert.ToInt32(txtSub000400_03.Text);
                    AddSydm.connection.address = HW_Net_API.ip2long(txtSub000400_02.Text, true); //修正所有API內有關IP的運算公式變成32位元版-允許負數 //把IP轉換函數從32位元版改回64位元版-不允許有負數

                    Identifier = "" + HW_Net_API.SYCG_addSYDM(AddSydm);
                    if (Convert.ToInt32(Identifier) > 0)
                    {

                        SQL = String.Format("INSERT INTO sydm (name, identifier, ip_address ,active ,api_key) VALUES ('{0}','{1}','{2}','{3}','{4}');", Name, Identifier, IP, active, Api_Key);
                        MySQL.InsertUpdateDelete(SQL);
                        initdgvSub0004_01();
                        labSub000400_02.ForeColor = Color.Black;
                        labSub000400_03.ForeColor = Color.Black;

                        HW_Net_API.SYCG_reloadSYDMList();
                        Leave_function();
                    }
                    else
                    {
                        MessageBox.Show(Language.m_StrbutSub000400_01Msg02, butSub000400_01.Text.Trim(), MessageBoxButtons.OK, MessageBoxIcon.Error);
                    }
                }
                else
                {
                    labSub000400_02.ForeColor = Color.Red;
                    labSub000400_03.ForeColor = Color.Red;
                    MessageBox.Show(Language.m_StrbutSub000400_01Msg01, butSub000400_01.Text.Trim(), MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
            }
            else
            {
                //---
                //製作SYDM編輯UI-串接修改SYDM API+ReLoad SYDM API
                SQL = String.Format("SELECT identifier FROM sydm WHERE id={0}",m_intSub000400id);
                MySqlDataReader DataReader = MySQL.GetDataReader(SQL);
                while (DataReader.Read())
                {
                    Identifier = DataReader["identifier"].ToString();
                    break;
                }
                DataReader.Close();

                Sydm_setActive setActive = new Sydm_setActive();
                Sydm_setSetup setSetup = new Sydm_setSetup();
                Sydm_setConnection setConnection = new Sydm_setConnection();
                setActive.identifier = Convert.ToInt32(Identifier);
                setActive.active = Convert.ToInt32(active);

                setSetup.identifier = Convert.ToInt32(Identifier);
                setSetup.setup.api_key = Api_Key;

                setConnection.identifier = Convert.ToInt32(Identifier);
                setConnection.connection.port = Convert.ToInt32(txtSub000400_03.Text);
                setConnection.connection.address = HW_Net_API.ip2long(txtSub000400_02.Text, true); //修正所有API內有關IP的運算公式變成32位元版-允許負數 //把IP轉換函數從32位元版改回64位元版-不允許有負數

                bool[] blncheck = new bool[3];
                blncheck[0] = HW_Net_API.SYCG_setSYDMActive(setActive);
                blncheck[1] = HW_Net_API.SYCG_setSYDMSetup(setSetup);
                blncheck[2] = HW_Net_API.SYCG_setSYDMConnection(setConnection);
                //---

                if (blncheck[0] & blncheck[1] & blncheck[2])
                {
                    SQL = String.Format("UPDATE sydm SET name='{0}',ip_address='{1}',active='{2}',api_key='{3}' WHERE id={4}", Name, IP, active, Api_Key, m_intSub000400id);
                    MySQL.InsertUpdateDelete(SQL);
                    initdgvSub0004_01();
                    labSub000400_02.ForeColor = Color.Black;
                    labSub000400_03.ForeColor = Color.Black;
                    HW_Net_API.SYCG_reloadSYDMList();
                    Leave_function();
                }
                else
                {
                    //---
                    //SYDM管理頁面中有舊SYDM資料但是SYCG為全新建立時，按下編修要自動幫他變成新增
                    //MessageBox.Show(Language.m_StrbutSub000400_01Msg02, butSub000400_01.Text.Trim(), MessageBoxButtons.OK, MessageBoxIcon.Error);
                    addSydm AddSydm = new addSydm();
                    AddSydm.active = Convert.ToInt32(active);
                    AddSydm.setup.api_key = Api_Key;
                    AddSydm.connection.port = Convert.ToInt32(txtSub000400_03.Text);
                    AddSydm.connection.address = HW_Net_API.ip2long(txtSub000400_02.Text, true); //修正所有API內有關IP的運算公式變成32位元版-允許負數 //把IP轉換函數從32位元版改回64位元版-不允許有負數

                    Identifier = "" + HW_Net_API.SYCG_addSYDM(AddSydm);
                    if (Convert.ToInt32(Identifier) > 0)
                    {
                        SQL = String.Format("UPDATE sydm SET name='{0}',ip_address='{1}',active='{2}',api_key='{3}',identifier='{4}' WHERE id={5}", Name, IP, active, Api_Key, Convert.ToInt32(Identifier), m_intSub000400id);
                        MySQL.InsertUpdateDelete(SQL);
                        initdgvSub0004_01();
                        labSub000400_02.ForeColor = Color.Black;
                        labSub000400_03.ForeColor = Color.Black;
                        HW_Net_API.SYCG_reloadSYDMList();
                        Leave_function();
                    }
                    else
                    {
                        MessageBox.Show(Language.m_StrbutSub000400_01Msg02, butSub000400_01.Text.Trim(), MessageBoxButtons.OK, MessageBoxIcon.Error);
                    }
                    //---SYDM管理頁面中有舊SYDM資料但是SYCG為全新建立時，按下編修要自動幫他變成新增    
                }
            }
        }
        //Sub000400_end
        
        //Sub0400_start
        private void cmbSub0400_00_SelectedIndexChanged(object sender, EventArgs e)//選定部門後連動改變人員選項
        {
            int i = cmbSub0400_00.SelectedIndex;
            if (m_ALDepartmentFP.Count >= 0)
            {
                Combo_SQLite_Date CSD = (Combo_SQLite_Date)(m_ALDepartmentFP[i]);
                GetComboData1(CSD.m_uid);
            }
        }

        private void cmbSub0400_01_SelectedIndexChanged(object sender, EventArgs e)//取出人員uid和對應卡片uid
        {
            int i = cmbSub0400_01.SelectedIndex;
            if (m_ALUserFP.Count >= 0 && i>-1)
            {
                Combo_SQLite_Date CSD = (Combo_SQLite_Date)(m_ALUserFP[i]);
                m_intNowUser_uidFP = CSD.m_uid;
                String StrSQL;

                StrSQL = String.Format("SELECT c.id AS uid,c.card_code AS Data FROM card AS c,card_for_user_car AS cfuc WHERE (c.id=cfuc.card_id) AND (cfuc.user_id={0}) ORDER BY c.id;", m_intNowUser_uidFP);
                MySqlDataReader DataReader = MySQL.GetDataReader(StrSQL);
                m_intNowCard_uidFP = -1;//預設沒有卡片
                m_ALCardFP.Clear();
                while (DataReader.Read())
                {
                    //m_intNowCard_uidFP = Convert.ToInt32(DataReader["uid"].ToString());
                    //break;
                    Combo_SQLite_Date CSD1 = new Combo_SQLite_Date();
                    CSD1.m_uid = Convert.ToInt32(DataReader["uid"].ToString());
                    CSD1.m_StrData = DataReader["Data"].ToString();
                    m_ALCardFP.Add(CSD1);
                }
                DataReader.Close();

                cmbSub0400_03.Items.Clear();
                if (m_ItemObject03FP != null)
                {
                    m_ItemObject03FP = null;
                }
                m_ItemObject03FP = new System.Object[m_ALCardFP.Count];
                for (int j = 0; j < m_ALCardFP.Count; j++)
                {
                    Combo_SQLite_Date CSD2 = (Combo_SQLite_Date)(m_ALCardFP[j]);
                    m_ItemObject03FP[j] = CSD2.m_StrData;
                }
                cmbSub0400_03.DropDownStyle = ComboBoxStyle.DropDownList;
                cmbSub0400_03.Items.AddRange(m_ItemObject03FP);

                m_intFingerprintCount = 0;//預設沒有指紋
                ClearFingerprintBuffer();//清空指紋
                StrSQL = String.Format("SELECT uid,FT_uid,Data FROM Fingerprint_List WHERE PL_uid={0};", m_intNowUser_uidFP);
                MySqlDataReader DataReader1 = MySQL.GetDataReader(StrSQL);
                while (DataReader1.Read())
                {
                    m_intFingerprintCount = Int32.Parse(DataReader1["FT_uid"].ToString()) - 1;
                    m_StrFingerprintData[m_intFingerprintCount] = DataReader1["Data"].ToString();
                    m_intFingerprintuid[m_intFingerprintCount] = Int32.Parse(DataReader1["uid"].ToString());
                    //m_intFingerprintCount++;
                }
                DataReader1.Close();

                ShowFingerprintImage();

            }
        }

        private void cmbSub0400_03_SelectedIndexChanged(object sender, EventArgs e)//取出卡片uid
        {
            if (m_ALCardFP.Count > 0)
            {
                int i = cmbSub0400_03.SelectedIndex;
                if (i >= 0)
                {
                    Combo_SQLite_Date CSD = (Combo_SQLite_Date)(m_ALCardFP[i]);
                    m_intNowCard_uidFP = CSD.m_uid;
                }
            }
        }

        private void imgSub0400_01_Click(object sender, EventArgs e)
        {
            Fingerprint ft = new Fingerprint(getCardInfo(1), "" + m_intNowUser_uidFP, "1");
            ft.ShowDialog();
            if (Fingerprint.m_blnrunSQL)
            {
                showSQLiteTable2GridView();
                m_ALFingerprint.Clear();
                m_StrFingerprintData[0] = "";
                ShowFingerprintImage();
                cmbSub0400_03.SelectedIndex = -1;
                cmbSub0400_04.SelectedIndex = -1;
            }
        }

        private void imgSub0400_02_Click(object sender, EventArgs e)
        {
            Fingerprint ft = new Fingerprint(getCardInfo(2), "" + m_intNowUser_uidFP, "2");
            ft.ShowDialog();
            if (Fingerprint.m_blnrunSQL)
            {
                showSQLiteTable2GridView();
                m_ALFingerprint.Clear();
                m_StrFingerprintData[1] = "";
                ShowFingerprintImage();
                cmbSub0400_03.SelectedIndex = -1;
                cmbSub0400_04.SelectedIndex = -1;
            }
        }

        private void imgSub0400_03_Click(object sender, EventArgs e)
        {
            Fingerprint ft = new Fingerprint(getCardInfo(3), "" + m_intNowUser_uidFP, "3");
            ft.ShowDialog();
            if (Fingerprint.m_blnrunSQL)
            {
                showSQLiteTable2GridView();
                m_ALFingerprint.Clear();
                m_StrFingerprintData[2] = "";
                ShowFingerprintImage();
                cmbSub0400_03.SelectedIndex = -1;
                cmbSub0400_04.SelectedIndex = -1;
            }
        }

        private void imgSub0400_04_Click(object sender, EventArgs e)
        {
            Fingerprint ft = new Fingerprint(getCardInfo(4), "" + m_intNowUser_uidFP, "4");
            ft.ShowDialog();
            if (Fingerprint.m_blnrunSQL)
            {
                showSQLiteTable2GridView();
                m_ALFingerprint.Clear();
                m_StrFingerprintData[3] = "";
                ShowFingerprintImage();
                cmbSub0400_03.SelectedIndex = -1;
                cmbSub0400_04.SelectedIndex = -1;
            }
        }

        private void imgSub0400_05_Click(object sender, EventArgs e)
        {
            Fingerprint ft = new Fingerprint(getCardInfo(5), "" + m_intNowUser_uidFP, "5");
            ft.ShowDialog();
            if (Fingerprint.m_blnrunSQL)
            {
                showSQLiteTable2GridView();
                m_ALFingerprint.Clear();
                m_StrFingerprintData[4] = "";
                ShowFingerprintImage();
                cmbSub0400_03.SelectedIndex = -1;
                cmbSub0400_04.SelectedIndex = -1;
            }
        }

        private void imgSub0400_06_Click(object sender, EventArgs e)
        {
            Fingerprint ft = new Fingerprint(getCardInfo(6), "" + m_intNowUser_uidFP, "6");
            ft.ShowDialog();
            if (Fingerprint.m_blnrunSQL)
            {
                showSQLiteTable2GridView();
                m_ALFingerprint.Clear();
                m_StrFingerprintData[5] = "";
                ShowFingerprintImage();
                cmbSub0400_03.SelectedIndex = -1;
                cmbSub0400_04.SelectedIndex = -1;
            }
        }

        private void imgSub0400_07_Click(object sender, EventArgs e)
        {
            Fingerprint ft = new Fingerprint(getCardInfo(7), "" + m_intNowUser_uidFP, "7");
            ft.ShowDialog();
            if (Fingerprint.m_blnrunSQL)
            {
                showSQLiteTable2GridView();
                m_ALFingerprint.Clear();
                m_StrFingerprintData[6] = "";
                ShowFingerprintImage();
                cmbSub0400_03.SelectedIndex = -1;
                cmbSub0400_04.SelectedIndex = -1;
            }
        }

        private void imgSub0400_08_Click(object sender, EventArgs e)
        {
            Fingerprint ft = new Fingerprint(getCardInfo(8), "" + m_intNowUser_uidFP, "8");
            ft.ShowDialog();
            if (Fingerprint.m_blnrunSQL)
            {
                showSQLiteTable2GridView();
                m_ALFingerprint.Clear();
                m_StrFingerprintData[7] = "";
                ShowFingerprintImage();
                cmbSub0400_03.SelectedIndex = -1;
                cmbSub0400_04.SelectedIndex = -1;
            }
        }

        private void imgSub0400_09_Click(object sender, EventArgs e)
        {
            Fingerprint ft = new Fingerprint(getCardInfo(9), "" + m_intNowUser_uidFP, "9");
            ft.ShowDialog();
            if (Fingerprint.m_blnrunSQL)
            {
                showSQLiteTable2GridView();
                m_ALFingerprint.Clear();
                m_StrFingerprintData[8] = "";
                ShowFingerprintImage();
                cmbSub0400_03.SelectedIndex = -1;
                cmbSub0400_04.SelectedIndex = -1;
            }
        }

        private void imgSub0400_10_Click(object sender, EventArgs e)
        {
            Fingerprint ft = new Fingerprint(getCardInfo(10), "" + m_intNowUser_uidFP, "10");
            ft.ShowDialog();
            if (Fingerprint.m_blnrunSQL)
            {
                showSQLiteTable2GridView();
                m_ALFingerprint.Clear();
                m_StrFingerprintData[9] = "";
                ShowFingerprintImage();
                cmbSub0400_03.SelectedIndex = -1;
                cmbSub0400_04.SelectedIndex = -1;
            }
        }

        private void dgvSub0400_00_SelectionChanged(object sender, EventArgs e)
        {
            if (m_blnloadFP)
            {
                try
                {
                    if (m_ALDepartmentFP.Count > 0)
                    {
                        int PD_uid = 0;
                        int P_uid = 0;
                        int index = dgvSub0400_00.SelectedRows[0].Index;//取得被選取的第一列位置    
                        PD_uid = Convert.ToInt32(dgvSub0400_00.Rows[index].Cells[0].Value.ToString());
                        P_uid = Convert.ToInt32(dgvSub0400_00.Rows[index].Cells[1].Value.ToString());
                        for (int i = 0; i < m_ALDepartmentFP.Count; i++)
                        {
                            Combo_SQLite_Date CSD = (Combo_SQLite_Date)(m_ALDepartmentFP[i]);
                            if (CSD.m_uid == PD_uid)
                            {
                                cmbSub0400_00.SelectedIndex = i;
                                break;
                            }
                        }
                        for (int i = 0; i < m_ALUserFP.Count; i++)
                        {
                            Combo_SQLite_Date CSD = (Combo_SQLite_Date)(m_ALUserFP[i]);
                            if (CSD.m_uid == P_uid)
                            {
                                cmbSub0400_01.SelectedIndex = i;
                                break;
                            }
                        }
                        showData2dgvSub0400_01(P_uid);//人員指紋列表-資料顯示
                    }
                }
                catch { }
            }
        }

        //---
        //撰寫新版指紋儲存函數SaveFingerprint在正確抓取織紋之後寫入DB之中
        public void SaveFingerprint(int fd_type)
        {
            String StrSQL = "";
            int PLuid=-1;
            //---
            //指紋UI支援反脅迫欄位
            int intDuress = -1;
            if (ckbSub0400_01.Checked)
            {
                intDuress = 1;
            }
            else
            {
                intDuress = 0;
            }
            //---指紋UI支援反脅迫欄位

            //---
            //把特徵碼寫入DB中
            

            StrSQL = "";
            if (m_ALFingerprint.Count > 0)
            {
                for (int i = 0; i < m_ALFingerprint.Count; i++)
                {
                    Fingerprint_SQLite_Date FSD = (Fingerprint_SQLite_Date)(m_ALFingerprint[i]);
                    if (FSD.m_uid == -1)
                    {
                        StrSQL += String.Format("INSERT INTO Fingerprint_List (PL_uid,CL_uid,FT_uid,Data,fd_type,duress) VALUES ('{0}','{1}','{2}','{3}','{4}','{5}');\n", FSD.m_PLuid, FSD.m_CLuid, FSD.m_FLuid, FSD.m_StrData, fd_type, intDuress);
                    }
                    else
                    {
                        StrSQL += String.Format("UPDATE Fingerprint_List SET PL_uid='{0}',CL_uid='{1}',FT_uid='{2}',Data='{3}',fd_type='{4}',duress='{5}' WHERE uid='{6}';\n", FSD.m_PLuid, FSD.m_CLuid, FSD.m_FLuid, FSD.m_StrData, fd_type, intDuress, FSD.m_uid);
                    }
                    PLuid = FSD.m_PLuid;
                }

                MySQL.InsertUpdateDelete(StrSQL);
                showSQLiteTable2GridView(PLuid);//新增/修改指紋時要指定列表的選擇位置
                m_ALFingerprint.Clear();
            }
            //---把特徵碼寫入DB中

            //---
            //取出人的ID
            Combo_SQLite_Date CSD = (Combo_SQLite_Date)(m_ALUserFP[cmbSub0400_01.SelectedIndex]);
            m_intNowUser_uidFP = CSD.m_uid;
            //---取出人的ID
            StrSQL="";
            /*
            //列出該人的所有卡片
            StrSQL = String.Format("SELECT c.id AS uid,c.card_code AS Data FROM card AS c,card_for_user_car AS cfuc WHERE (c.id=cfuc.card_id) AND (cfuc.user_id={0}) ORDER BY c.id;", m_intNowUser_uidFP);
            MySqlDataReader DataReader = MySQL.GetDataReader(StrSQL);
            m_intNowCard_uidFP = -1;//預設沒有卡片
            m_ALCardFP.Clear();
            while (DataReader.Read())
            {
                //m_intNowCard_uidFP = Convert.ToInt32(DataReader["uid"].ToString());
                //break;
                Combo_SQLite_Date CSD1 = new Combo_SQLite_Date();
                CSD1.m_uid = Convert.ToInt32(DataReader["uid"].ToString());
                CSD1.m_StrData = DataReader["Data"].ToString();
                m_ALCardFP.Add(CSD1);
            }
            DataReader.Close();
            */ 
            //---
            //取出該人的所有指紋
            m_intFingerprintCount = 0;//預設沒有指紋
            ClearFingerprintBuffer();//清空指紋
            StrSQL = String.Format("SELECT uid,FT_uid,Data FROM Fingerprint_List WHERE PL_uid={0};", m_intNowUser_uidFP);
            MySqlDataReader DataReader1 = MySQL.GetDataReader(StrSQL);
            while (DataReader1.Read())
            {
                m_intFingerprintCount = Int32.Parse(DataReader1["FT_uid"].ToString()) - 1;
                m_StrFingerprintData[m_intFingerprintCount] = DataReader1["Data"].ToString();
                m_intFingerprintuid[m_intFingerprintCount] = Int32.Parse(DataReader1["uid"].ToString());
                //m_intFingerprintCount++;
            }
            DataReader1.Close();
            //---取出該人的所有指紋
        }
        //---撰寫新版指紋儲存函數SaveFingerprint在正確抓取織紋之後寫入DB之中
        
        private void butSub0400_01_Click(object sender, EventArgs e)//抓取指紋
        {
            String USB_Template = "";//為了整合USB指紋抓取額外宣告的變數
            String StrDiviceName = "";
            int intDiviceIndex = -1;

            if ((cmbSub0400_00.SelectedIndex > -1) && (cmbSub0400_01.SelectedIndex > -1) && (cmbSub0400_02.SelectedIndex > -1) && (cmbSub0400_03.SelectedIndex > -1) && (cmbSub0400_04.SelectedIndex > -1))
            {
                labSub0400_11.ForeColor = Color.Black;
                labSub0400_12.ForeColor = Color.Black;
                labSub0400_13.ForeColor = Color.Black;
                labSub0400_14.ForeColor = Color.Black;
                labSub0400_15.ForeColor = Color.Black;
            }
            else
            {
                labSub0400_11.ForeColor = Color.Black;
                labSub0400_12.ForeColor = Color.Black;
                labSub0400_13.ForeColor = Color.Black;
                labSub0400_14.ForeColor = Color.Black;
                labSub0400_15.ForeColor = Color.Black;

                if (cmbSub0400_00.SelectedIndex == -1)
                {
                    labSub0400_12.ForeColor = Color.Red;
                }
                if (cmbSub0400_01.SelectedIndex == -1)
                {
                    labSub0400_11.ForeColor = Color.Red;
                }
                if (cmbSub0400_02.SelectedIndex == -1)
                {
                    labSub0400_13.ForeColor = Color.Red;
                }
                if (cmbSub0400_03.SelectedIndex == -1)
                {
                    labSub0400_14.ForeColor = Color.Red;
                }
                if (cmbSub0400_04.SelectedIndex == -1)
                {
                    labSub0400_15.ForeColor = Color.Red;
                }
                return;
            }

            int intState;
            byte[] cmd_test = new byte[3];

            intState = -1;
            cmd_test[0] = 0x02;//<STX>
            cmd_test[1] = 0x01;//LEN
            cmd_test[2] = 0x0D;//SN

            m_serialPort1.BaudRate = 9600;
            m_serialPort1.Parity = Parity.None;
            m_serialPort1.DataBits = 8;
            m_serialPort1.StopBits = StopBits.One;

            try
            {
                //---
                //修改抓取指紋設備種類判斷方式
                StrDiviceName = cmbSub0400_02.SelectedItem.ToString();
                intDiviceIndex=-1;
                //if (cmbSub0400_02.SelectedItem.ToString() != "USB")//執行舊版動作
                if (StrDiviceName.Contains("COM"))
                //---修改抓取指紋設備種類判斷方式
                {
                    if (m_serialPort1.IsOpen)
                    {
                        m_serialPort1.Close();
                    }
                    m_serialPort1.PortName = cmbSub0400_02.SelectedItem.ToString();
                    m_serialPort1.Open();
                    m_serialPort1.Write(cmd_test, 0, cmd_test.Length);
                    Thread.Sleep(m_delay);
                    if (m_serialPort1.BytesToRead > 0)
                    {
                        Array.Clear(m_byteResponseFP, 0, m_byteResponseFP.Length);
                        int len = m_serialPort1.Read(m_byteResponseFP, 0, m_byteResponseFP.Length);
                        if (len > 4)
                        {
                            if (m_byteResponseFP[0] == 0x02 && m_byteResponseFP[2] == 0x0D && m_byteResponseFP[3] == 0x00)
                            {
                                intState = 1;//確定是RD300
                                int count = 0;
                                byte[] cmd_set = new byte[20];

                                count = 0;
                                Array.Clear(cmd_set, 0, cmd_set.Length);
                                cmd_set[count++] = 0x02;//不自動送
                                cmd_set[count++] = 0x03;
                                cmd_set[count++] = 0x03;
                                cmd_set[count++] = 0x01;
                                cmd_set[count++] = 0x02;
                                m_serialPort1.Write(cmd_set, 0, count);
                                Thread.Sleep(m_delay);
                                Array.Clear(m_byteResponseFP, 0, m_byteResponseFP.Length);
                                m_serialPort1.Read(m_byteResponseFP, 0, m_byteResponseFP.Length);

                                count = 0;
                                Array.Clear(cmd_set, 0, cmd_set.Length);
                                cmd_set[count++] = 0x02;//不自動讀指紋
                                cmd_set[count++] = 0x0D;
                                cmd_set[count++] = 0x65;
                                cmd_set[count++] = 0x00;
                                cmd_set[count++] = 0x00;
                                cmd_set[count++] = 0x00;
                                cmd_set[count++] = 0x00;
                                cmd_set[count++] = 0x00;
                                cmd_set[count++] = 0x00;
                                cmd_set[count++] = 0x00;
                                cmd_set[count++] = 0x00;
                                cmd_set[count++] = 0x00;
                                cmd_set[count++] = 0x00;
                                cmd_set[count++] = 0x00;
                                cmd_set[count++] = 0x00;
                                m_serialPort1.Write(cmd_set, 0, count);
                                Thread.Sleep(m_delay);
                                Array.Clear(m_byteResponseFP, 0, m_byteResponseFP.Length);
                                m_serialPort1.Read(m_byteResponseFP, 0, m_byteResponseFP.Length);

                            }
                            else
                            {
                                intState = 0;//RS232設備無回應
                            }
                        }
                        else
                        {
                            intState = 0;//RS232設備無回應
                        }
                    }
                    else
                    {
                        intState = 0;//RS232設備無回應
                    }
                }
                else//=USB 執行新版動作
                {
                    //---
                    //刪除上一次的指紋特徵檔
                    String StrTmpFile = System.Windows.Forms.Application.StartupPath;
                    StrTmpFile += "\\Template.mfc";
                    if (System.IO.File.Exists(StrTmpFile))
                    {
                        try
                        {
                            System.IO.File.Delete(StrTmpFile);
                        }
                        catch { }
                    }
                    //---刪除上一次的指紋特徵檔

                    //---
                    //呼叫執行執行MFC
                    Process mfc_pro;
                    mfc_pro = null;
                    //---
                    //修改抓取指紋設備種類判斷方式
                    for (int i = 0; i < HW_Net_API.m_ALFDT_Name.Count; i++)
                    {
                        if (StrDiviceName == HW_Net_API.m_ALFDT_Name[i].ToString())
                        {
                            intDiviceIndex = Convert.ToInt32(HW_Net_API.m_ALFDT_ID[i].ToString());
                            break;
                        }
                    }
                    switch(intDiviceIndex)
                    {
                        case 1:
                            mfc_pro = Process.Start(System.Windows.Forms.Application.StartupPath + "\\GetFingerprint.exe");//整合suprema指紋機程式
                            break;
                        case 2:
                            mfc_pro = Process.Start(System.Windows.Forms.Application.StartupPath + "\\Get_Fingerprint.exe");
                            break;
                    }
                    //---呼叫執行執行MFC

                    //---
                    //等愛MFC執行結束
                    do
                    {
                        Thread.Sleep(1000);
                    }
                    while ((mfc_pro != null) && (mfc_pro.HasExited == false));

                    if (mfc_pro != null)
                    {
                        mfc_pro.Close();//把mfc_pro清空被執行程式的資源,但m_pro實體存在
                        mfc_pro = null;// 清空mfc_pro實體
                    }
                    //---等愛MFC執行結束

                    //---
                    //讀取指紋特徵檔
                    StreamReader sr = new StreamReader(StrTmpFile);
                    while (!sr.EndOfStream)// 每次讀取一行，直到檔尾
                    {
                        USB_Template = sr.ReadLine();// 讀取文字到 line 變數
                        break;
                    }
                    sr.Close();// 關閉串流
                    if (USB_Template.Length > 0)
                    {
                        intState = 1;
                    }
                    else
                    {
                        intState = -1;//錯誤
                    }
                    //---讀取指紋特徵檔

                }
            }
            catch
            {
                intState = -1;//RS232錯誤
            }
            switch (intState)
            {
                case 0:
                    /*//停用RS232的程式碼
                    MessageBox.Show(Language.m_StrFingerprint_Msg03, Language.m_Strtr3_Item01, MessageBoxButtons.OK, MessageBoxIcon.Error);//MessageBox.Show("設備異常");
                    m_serialPort1.Close();
                    */ 
                    break;
                case 1:
                    //MessageBox.Show("connect ok");
                    if (StrDiviceName.Contains("COM"))//執行舊版動作
                    {
                        /*//停用RS232的程式碼
                        Animation.createThreadAnimation(Language.m_StrFingerprint_Msg01, Animation.Animation_Wait_RS232_Fingerprint);
                        if (Animation.m_intSYFCLibAns == 3)
                        {
                            String buf = "5A,A5,01,00," + ToHexString(m_byteTemplate, m_byteTemplate.Length);
                            if (cmbSub0400_04.SelectedIndex >= 0)
                            {
                                m_intFingerprintCount = cmbSub0400_04.SelectedIndex;
                            }
                            else
                            {
                                m_intFingerprintCount = 0;
                            }

                            m_StrFingerprintData[m_intFingerprintCount] = buf.Substring(0, buf.Length - 1);
                            Fingerprint_SQLite_Date FSD = new Fingerprint_SQLite_Date();
                            FSD.m_uid = m_intFingerprintuid[m_intFingerprintCount];
                            FSD.m_PLuid = m_intNowUser_uidFP;
                            FSD.m_FLuid = m_intFingerprintCount + 1;
                            FSD.m_CLuid = m_intNowCard_uidFP;
                            FSD.m_StrData = m_StrFingerprintData[m_intFingerprintCount];
                            m_ALFingerprint.Add(FSD);

                            //m_intFingerprintCount++;
                            ShowFingerprintImage();
                            m_serialPort1.Close();
                        }
                        */ 
                    }
                    else//=USB 執行新版動作
                    {
                        if (cmbSub0400_04.SelectedIndex >= 0)
                        {
                            m_intFingerprintCount = cmbSub0400_04.SelectedIndex;
                        }
                        else
                        {
                            m_intFingerprintCount = 0;
                        }

                        m_StrFingerprintData[m_intFingerprintCount] = USB_Template;
                        Fingerprint_SQLite_Date FSD = new Fingerprint_SQLite_Date();
                        FSD.m_uid = m_intFingerprintuid[m_intFingerprintCount];
                        FSD.m_PLuid = m_intNowUser_uidFP;
                        FSD.m_FLuid = m_intFingerprintCount + 1;
                        FSD.m_CLuid = m_intNowCard_uidFP;
                        FSD.m_StrData = m_StrFingerprintData[m_intFingerprintCount];
                        m_ALFingerprint.Add(FSD);

                        //m_intFingerprintCount++;
                        ShowFingerprintImage();
                        SaveFingerprint(intDiviceIndex);

                        //---
                        //要有紀錄最後一次抓指紋的使用機型功能，下次顯示時要能自動提醒
                        StreamWriter sw = new StreamWriter(Application.StartupPath + @"\FD_used.set");
                        sw.WriteLine("" + intDiviceIndex);// 寫入文字
                        sw.Close();// 關閉串流
                        //---要有紀錄最後一次抓指紋的使用機型功能，下次顯示時要能自動提醒
                    }
                    break;
                case -1:
                    MessageBox.Show(Language.m_StrFingerprintMsg00, butSub0400_01.Text, MessageBoxButtons.OK, MessageBoxIcon.Error);//MessageBox.Show("設備異常");
                    break;
            }
        }

        private void butSub0400_02_Click(object sender, EventArgs e)//刪除指紋按鈕
        {
            String SQL = "";
            ArrayList ALID = new ArrayList();
            ALID.Clear();
            for (int i = 0; i < dgvSub0400_01.Rows.Count; i++)
            {
                String data = dgvSub0400_01.Rows[i].Cells[1].Value.ToString().ToLower();//抓取DataGridView欄位資料
                if (data == "true")
                {
                    ALID.Add(dgvSub0400_01.Rows[i].Cells[0].Value.ToString());//抓 ID
                }
            }
            for (int j = 0; j < ALID.Count; j++)
            {
                SQL+=String.Format("DELETE FROM fingerprint_list WHERE uid={0};",ALID[j].ToString());
            }

            if (SQL.Length > 0)
            {
                MySQL.InsertUpdateDelete(SQL);
                showSQLiteTable2GridView();
            }
        }

        private void butSub0400_03_Click(object sender, EventArgs e)//指紋UI可以新增卡片功能
        {
            if (cmbSub0400_01.SelectedIndex > -1)//確定有選人
            {
                String UserName = cmbSub0400_01.SelectedItem.ToString();
                m_TPOld = m_tabMain.SelectedTab;//--2017/02/22 製作返回按鈕功能
                TabPage_Push();//m_StackTPOld.Push(m_TPOld);//--2017/02/22 製作返回按鈕功能

                m_tabSub010200.Parent = m_tabMain;
                labSub010200_08.ReadOnly = false;//可編輯卡片內碼
                m_intcard_id = -10;
                initSub010200UI(UserName);//人和車輛UI在立即配發卡片時利用程式手法直接顯示對應持有人名稱(原本要DB有資料才關聯出來) initSub010200UI();
                m_tabMain.SelectedTab = m_tabSub010200;

                m_Sub010200ALInit.Clear();
                m_Sub010200ALInit.Add(labSub010200_07.Text);
                m_Sub010200ALInit.Add(labSub010200_08.Text);
                m_Sub010200ALInit.Add(txtSub010200_01.Text);
                m_Sub010200ALInit.Add(txtSub010200_02.Text);
                m_Sub010200ALInit.Add(txtSub010200_03.Text);
                m_Sub010200ALInit.Add(cmbSub010200_01.SelectedIndex + "");
                m_Sub010200ALInit.Add(adpSub010200_01.Value.ToString("yyyy-MM-dd HH:mm"));
                m_Sub010200ALInit.Add(adpSub010200_02.Value.ToString("yyyy-MM-dd HH:mm"));
                m_Sub010200ALInit.Add(ckbSub010200_01.Checked.ToString());
                m_Sub010200ALInit.Add(ckbSub010200_02.Checked.ToString());
                m_Sub010200ALInit.Add(ckbSub010200_03.Checked.ToString());
                m_Sub010200ALInit.Add(ckbSub010200_04.Checked.ToString());
                m_Sub010200ALInit.Add(ckbSub010200_05.Checked.ToString());
                m_Sub010200ALInit.Add(ckbSub010200_06.Checked.ToString());
                m_Sub010200ALInit.Add(ckbSub010200_07.Checked.ToString());
                m_Sub010200ALInit.Add(ckbSub010200_08.Checked.ToString());
                m_Sub010200ALInit.Add(ckbSub010200_09.Checked.ToString());
                m_Sub010200ALInit.Add(ckbSub010200_10.Checked.ToString());
                m_Sub010200ALInit.Add(ckbSub010200_11.Checked.ToString());
                m_Sub010200ALInit.Add(rdbSub010200_01.Checked.ToString());
                m_Sub010200ALInit.Add(rdbSub010200_02.Checked.ToString());
                m_Sub010200ALInit.Add(rdbSub010200_03.Checked.ToString());
                m_Sub010200ALInit.Add(rdbSub010200_04.Checked.ToString());
                m_Sub010200ALInit.Add(steSub010200_01.StrValue1 + steSub010200_01.StrValue2);
                m_Sub010200ALInit.Add(steSub010200_02.StrValue1 + steSub010200_02.StrValue2);
                m_Sub010200ALInit.Add(steSub010200_03.StrValue1 + steSub010200_03.StrValue2);

                labSub010200_08.Focus();
                m_intUserAddCard_id = 0;
                m_intUserAddCard_id = m_intNowUser_uidFP;
            }
            else
            {
                MessageBox.Show("Fail");
            }
        }

        private void dgvSub0400_01_SelectionChanged(object sender, EventArgs e)//人員指紋列表-選擇改變時，其他元件資料顯示連動改變
        {
            //---
            //列表元件shift全選時最前面的狀態要能自動打勾，反之要能取消
            for (int i = 0; i < dgvSub0400_01.Rows.Count; i++)
            {
                dgvSub0400_01.Rows[i].Cells[1].Value = false;
            }
            for (int j = 0; j < dgvSub0400_01.SelectedRows.Count; j++)
            {
                dgvSub0400_01.SelectedRows[j].Cells[1].Value = true;
            }
            //---列表元件shift全選時最前面的狀態要能自動打勾，反之要能取消

            try
            {
                if (m_ALDepartmentFP.Count > 0)
                {
                    int index = dgvSub0400_01.SelectedRows[0].Index;//取得被選取的第一列位置
                    if (index >= 0)
                    {
                        //String Str00 = dgvSub0400_01.Rows[index].Cells[0].Value.ToString();//隱藏的指紋列表id
                        String Str02 = dgvSub0400_01.Rows[index].Cells[2].Value.ToString();//卡號
                        String Str03 = dgvSub0400_01.Rows[index].Cells[3].Value.ToString();//反脅迫
                        String Str04 = dgvSub0400_01.Rows[index].Cells[4].Value.ToString();//指紋編號
                        String Str05 = dgvSub0400_01.Rows[index].Cells[5].Value.ToString();//設備型號

                        for (int i = 0; i < cmbSub0400_03.Items.Count; i++)//卡號
                        {
                            cmbSub0400_03.SelectedIndex = -1;
                            if (Str02 == cmbSub0400_03.Items[i].ToString())
                            {
                                cmbSub0400_03.SelectedIndex = i;
                                break;
                            }
                        }

                        for (int j = 0; j < cmbSub0400_02.Items.Count; j++)
                        {
                            cmbSub0400_02.SelectedIndex = -1;
                            if (Str05 == cmbSub0400_02.Items[j].ToString())
                            {
                                cmbSub0400_02.SelectedIndex = j;
                                break;
                            }
                        }

                        for (int k = 0; k < cmbSub0400_04.Items.Count; k++)//指紋編號
                        {
                            cmbSub0400_04.SelectedIndex = -1;
                            if (Str04 == cmbSub0400_04.Items[k].ToString())
                            {
                                cmbSub0400_04.SelectedIndex = k;
                                break;
                            }
                        }

                        if (Str03 == "1")//反脅迫
                        {
                            ckbSub0400_01.Checked = true;
                        }
                        else
                        {
                            ckbSub0400_01.Checked = false;
                        }          		
                    }

                }
            }
            catch { }
        }

        private void tvmSub0203_01_MouseDown(object sender, MouseEventArgs e)
        {
            tvmSub0203_01.SelectedNode = null;//修正區域門區群組建立後無法跳至最上層，除非新增區域在取消 [授權結果UI也一併修改]
        }

        private void tvmSub0203_02_MouseDown(object sender, MouseEventArgs e)
        {
            tvmSub0203_02.SelectedNode = null;//修正區域門區群組建立後無法跳至最上層，除非新增區域在取消 [授權結果UI也一併修改]
        }

        //Sub0400_end

        private void m_tabMain_KeyUp(object sender, KeyEventArgs e)
        {
            //---
            //每個分頁要支援ESC關閉功能
            if (e.KeyCode == Keys.Escape)
            {
                if (m_tabMain.SelectedTab == m_tabPMain00)
                {
                    Leave_function();
                    return;//修正ESC關閉功能分頁功能，避免其他分頁也跟著關閉形成誤動作
                }
                if (m_tabMain.SelectedTab == m_tabPMain01)
                {
                    Leave_function();
                    return;//修正ESC關閉功能分頁功能，避免其他分頁也跟著關閉形成誤動作
                }
                if (m_tabMain.SelectedTab == m_tabPMain02)
                {
                    Leave_function();
                    return;//修正ESC關閉功能分頁功能，避免其他分頁也跟著關閉形成誤動作
                }
                if (m_tabMain.SelectedTab == m_tabPMain03)
                {
                    Leave_function();
                    return;//修正ESC關閉功能分頁功能，避免其他分頁也跟著關閉形成誤動作
                }
                if (m_tabMain.SelectedTab == m_tabPMain04)
                {
                    Leave_function();
                    return;//修正ESC關閉功能分頁功能，避免其他分頁也跟著關閉形成誤動作
                }
                if (m_tabMain.SelectedTab == m_tabSub0000)
                {
                    Leave_function();
                    return;//修正ESC關閉功能分頁功能，避免其他分頁也跟著關閉形成誤動作
                }
                if (m_tabMain.SelectedTab == m_tabSub000001)
                {
                    butSub000001_14.PerformClick();
                    return;//修正ESC關閉功能分頁功能，避免其他分頁也跟著關閉形成誤動作
                }
                if (m_tabMain.SelectedTab == m_tabSub0003)
                {
                    Leave_function();
                    return;//修正ESC關閉功能分頁功能，避免其他分頁也跟著關閉形成誤動作
                }
                if (m_tabMain.SelectedTab == m_tabSub000301)
                {
                    butSub000301_23.PerformClick();
                    return;//修正ESC關閉功能分頁功能，避免其他分頁也跟著關閉形成誤動作
                }
                if (m_tabMain.SelectedTab == m_tabSub0001)
                {
                    butSub0001_14.PerformClick();
                    return;//修正ESC關閉功能分頁功能，避免其他分頁也跟著關閉形成誤動作
                }
                if (m_tabMain.SelectedTab == m_tabSub000100)
                {
                    butSub000100_09.PerformClick();
                    return;//修正ESC關閉功能分頁功能，避免其他分頁也跟著關閉形成誤動作
                }
                if (m_tabMain.SelectedTab == m_tabSub0002)
                {
                    Leave_function();
                    return;//修正ESC關閉功能分頁功能，避免其他分頁也跟著關閉形成誤動作
                }
                if (m_tabMain.SelectedTab == m_tabSub000200)
                {
                    butSub000200_20.PerformClick();
                    return;//修正ESC關閉功能分頁功能，避免其他分頁也跟著關閉形成誤動作
                }
                if (m_tabMain.SelectedTab == m_tabSub0004)
                {
                    Leave_function();
                    return;//修正ESC關閉功能分頁功能，避免其他分頁也跟著關閉形成誤動作
                }
                if (m_tabMain.SelectedTab == m_tabSub000400)
                {
                    butSub000400_00.PerformClick();
                    return;//修正ESC關閉功能分頁功能，避免其他分頁也跟著關閉形成誤動作
                }
                if (m_tabMain.SelectedTab == m_tabSub0100)
                {
                    Leave_function();
                    return;//修正ESC關閉功能分頁功能，避免其他分頁也跟著關閉形成誤動作
                }
                if (m_tabMain.SelectedTab == m_tabSub010000)
                {
                    butSub010000_15.PerformClick();
                    return;//修正ESC關閉功能分頁功能，避免其他分頁也跟著關閉形成誤動作
                }
                if (m_tabMain.SelectedTab == m_tabSub0101)
                {
                    Leave_function();
                    return;//修正ESC關閉功能分頁功能，避免其他分頁也跟著關閉形成誤動作
                }
                if (m_tabMain.SelectedTab == m_tabSub010100)
                {
                    butSub010100_15.PerformClick();
                    return;//修正ESC關閉功能分頁功能，避免其他分頁也跟著關閉形成誤動作
                }
                if (m_tabMain.SelectedTab == m_tabSub0102)
                {
                    Leave_function();
                    return;//修正ESC關閉功能分頁功能，避免其他分頁也跟著關閉形成誤動作
                }
                if (m_tabMain.SelectedTab == m_tabSub010200)
                {
                    butSub010200_07.PerformClick();
                    return;//修正ESC關閉功能分頁功能，避免其他分頁也跟著關閉形成誤動作
                }
                if (m_tabMain.SelectedTab == m_tabSub0103)
                {
                    Leave_function();
                    return;//修正ESC關閉功能分頁功能，避免其他分頁也跟著關閉形成誤動作
                }
                if (m_tabMain.SelectedTab == m_tabSub0104)
                {
                    Leave_function();
                    return;//修正ESC關閉功能分頁功能，避免其他分頁也跟著關閉形成誤動作
                }
                if (m_tabMain.SelectedTab == m_tabSub010400)
                {
                    butSub010400_20.PerformClick();
                    return;//修正ESC關閉功能分頁功能，避免其他分頁也跟著關閉形成誤動作
                }
                if (m_tabMain.SelectedTab == m_tabSub0200)
                {
                    Leave_function();
                    return;//修正ESC關閉功能分頁功能，避免其他分頁也跟著關閉形成誤動作
                }
                if (m_tabMain.SelectedTab == m_tabSub020000)
                {
                    butSub020000_24.PerformClick();
                    return;//修正ESC關閉功能分頁功能，避免其他分頁也跟著關閉形成誤動作
                }
                if (m_tabMain.SelectedTab == m_tabSub0201)
                {
                    Leave_function();
                    return;//修正ESC關閉功能分頁功能，避免其他分頁也跟著關閉形成誤動作
                }
                if (m_tabMain.SelectedTab == m_tabSub0202)
                {
                    Leave_function();
                    return;//修正ESC關閉功能分頁功能，避免其他分頁也跟著關閉形成誤動作
                }
                if (m_tabMain.SelectedTab == m_tabSub0203)
                {
                    Leave_function();
                    return;//修正ESC關閉功能分頁功能，避免其他分頁也跟著關閉形成誤動作
                }
                if (m_tabMain.SelectedTab == m_tabSub020300)
                {
                    Leave_function();
                    return;//修正ESC關閉功能分頁功能，避免其他分頁也跟著關閉形成誤動作
                }
                if (m_tabMain.SelectedTab == m_tabSub0300)
                {
                    Leave_function();
                    return;//修正ESC關閉功能分頁功能，避免其他分頁也跟著關閉形成誤動作
                }
                if (m_tabMain.SelectedTab == m_tabSub0400)
                {
                    Leave_function();
                    return;//修正ESC關閉功能分頁功能，避免其他分頁也跟著關閉形成誤動作
                }
            }
            //---每個分頁要支援ESC關閉功能

            //---
            //控制器UI多選編輯實作 ~ 撰寫對應鍵盤事件
            if (m_tabMain.SelectedTab == m_tabSub000001)
            {
                if ((e.KeyCode == Keys.Left) || (e.KeyCode == Keys.Right))
                {
                    if (e.KeyCode == Keys.Left)
                    {
                        if (m_intControllerIndex > 0)
                        {
                            m_intControllerIndex--;
                        }
                        else
                        {
                            m_intControllerIndex = 0;
                        }
                    }

                    if (e.KeyCode == Keys.Right)
                    {
                        if (m_intControllerIndex < (m_ALControllerObj.Count - 1))
                        {
                            m_intControllerIndex++;
                        }
                        else
                        {
                            if (m_ALControllerObj.Count > 0)
                            {
                                m_intControllerIndex = m_ALControllerObj.Count - 1;
                            }
                        }
                    }

                    m_intdgvSub0000_01_SN = Int32.Parse(m_ALControllerObj[m_intControllerIndex].ToString());
                    m_intcontroller_sn = m_intdgvSub0000_01_SN;
                    
                    setSub000001UI();
                }
            }
            //---控制器UI多選編輯實作 ~ 撰寫對應鍵盤事件

            //---
            //製作多選支援左右鍵切換查詢+修改門區內容 ~ 撰寫對應鍵盤事件
            if (m_tabMain.SelectedTab == m_tabSub000100)
            {
                if ((e.KeyCode == Keys.Left) || (e.KeyCode == Keys.Right))
                {
                    if (e.KeyCode == Keys.Left)
                    {
                        if (m_intDoorIndex > 0)
                        {
                            m_intDoorIndex--;
                        }
                        else
                        {
                            m_intDoorIndex = 0;
                        }
                    }

                    if (e.KeyCode == Keys.Right)
                    {
                        if (m_intDoorIndex < (m_ALDoorObj.Count - 1))
                        {
                            m_intDoorIndex++;
                        }
                        else
                        {
                            if (m_ALDoorObj.Count > 0)
                            {
                                m_intDoorIndex = m_ALDoorObj.Count - 1;
                            }
                        }
                    }

                    Tree_Node tmp = (Tree_Node)m_ALDoorObj[m_intDoorIndex];
                    ShowtabSub000100UI(tmp.m_id, tmp.m_unit, Int32.Parse(tmp.m_data), tmp.Text);
                }
            }
            //---製作多選支援左右鍵切換查詢+修改門區內容 ~ 撰寫對應鍵盤事件

            //---
            //製作多選查詢授權紀錄-左右鍵切換觀看指定授權內容 ~ 撰寫對應鍵盤事件
            if (m_tabMain.SelectedTab == m_tabSub020300)
            {
                if ((e.KeyCode == Keys.Left) || (e.KeyCode == Keys.Right))
                {
                    if (e.KeyCode == Keys.Left)
                    {
                        if (m_intAuthIndex > 0)
                        {
                            m_intAuthIndex--;
                        }
                        else
                        {
                            m_intAuthIndex = 0;
                        }
                    }

                    if (e.KeyCode == Keys.Right)
                    {
                        if (m_intAuthIndex < (m_ALAuthObj.Count - 1))
                        {
                            m_intAuthIndex++;
                        }
                        else
                        {
                            if (m_ALAuthObj.Count > 0)
                            {
                                m_intAuthIndex = m_ALAuthObj.Count - 1;
                            }
                        }
                    }

                    Tree_Node tmp = (Tree_Node)m_ALAuthObj[m_intAuthIndex];
                    CleanSub020300UIVar();
                    if (tmp.m_data == "door")
                    {
                        SetSub020300UIVar(1, tmp.Text, tmp.m_id);//門
                    }
                    else
                    {
                        SetSub020300UIVar(0, tmp.Text, tmp.m_id);//人(卡)
                    }
                    initSub020300UI();
                }
            }
            //---製作多選查詢授權紀錄-左右鍵切換觀看指定授權內容 ~ 撰寫對應鍵盤事件
        }

        //---
        //將所有列表UI上的搜尋文字框都加上enter鍵的功能
        private void txtSub0000_01_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.KeyCode == Keys.Enter)
            {
                butSub0000_10.PerformClick();
            }
        }

        private void txtSub0003_01_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.KeyCode == Keys.Enter)
            {
                butSub0003_10.PerformClick();
            }
        }

        private void txtSub000301_04_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.KeyCode == Keys.Enter)
            {
                butSub000301_22.PerformClick();
            }
        }

        private void txtSub0002_01_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.KeyCode == Keys.Enter)
            {
                butSub0002_09.PerformClick();
            }
        }

        private void txtSub0100_01_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.KeyCode == Keys.Enter)
            {
                butSub0100_09.PerformClick();
            }
        }

        private void txtSub0101_01_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.KeyCode == Keys.Enter)
            {
                butSub0101_09.PerformClick();
            }
        }

        private void txtSub0102_01_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.KeyCode == Keys.Enter)
            {
                butSub0102_09.PerformClick();
            }
        }

        private void txtSub0104_01_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.KeyCode == Keys.Enter)
            {
                butSub0104_09.PerformClick();
            }
        }

        private void txtSub0200_01_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.KeyCode == Keys.Enter)
            {
                butSub0200_09.PerformClick();
            }
        }

        //---將所有列表UI上的搜尋文字框都加上enter鍵的功能

        //---
        //門區設定要支援滑鼠雙點擊功能
        private void tvmSub0001_01_DoubleClick(object sender, EventArgs e)
        {
            butSub0001_05.PerformClick();
        }

        private void ltvSub0001_01_DoubleClick(object sender, EventArgs e)
        {
            butSub0001_12.PerformClick();
        }

        //---門區設定要支援滑鼠雙點擊功能

        //---
        //門區授權查詢要支援滑鼠雙點擊功能
        public Tree_Node TN_tvmSub0203_01;
        public Tree_Node TN_tvmSub0203_02;

        private void tvmSub0203_01_DoubleClick(object sender, EventArgs e)
        {
            butSub0203_02.PerformClick();
        }

        private void tvmSub0203_02_DoubleClick(object sender, EventArgs e)
        {
            butSub0203_03.PerformClick();
        }

        //---門區授權查詢要支援滑鼠雙點擊功能

        //---
        //製作多選查詢授權紀錄-左右鍵切換觀看指定授權內容 ~ 設計兩個切換按鈕
        private void ChangeAuthIndex_Click(object sender, EventArgs e)
        {
            if (((Button)sender).Text == "<")
            {
                if (m_intAuthIndex > 0)
                {
                    m_intAuthIndex--;
                }
                else
                {
                    m_intAuthIndex = 0;
                }
            }

            if (((Button)sender).Text == ">")
            {
                if (m_intAuthIndex < (m_ALAuthObj.Count - 1))
                {
                    m_intAuthIndex++;
                }
                else
                {
                    m_intAuthIndex = m_ALAuthObj.Count - 1;
                }
            }

            Tree_Node tmp = (Tree_Node)m_ALAuthObj[m_intAuthIndex];
            CleanSub020300UIVar();
            if (tmp.m_data == "door")
            {
                SetSub020300UIVar(1, tmp.Text, tmp.m_id);//門
            }
            else
            {
                SetSub020300UIVar(0, tmp.Text, tmp.m_id);//人(卡)
            }
            initSub020300UI();
        }
        //---製作多選查詢授權紀錄-左右鍵切換觀看指定授權內容 ~ 設計兩個切換按鈕

        //---
        //人員編輯內頁的卡片列表中增加按鈕，用來實現人員+配卡+授權功能 ~ 增加對應按鈕對應事件
        private void dgvSub010000_01_CellClick(object sender, DataGridViewCellEventArgs e)
        {
            if (e.ColumnIndex == 5)
            {
                DataGridViewTextBoxCell cell = (DataGridViewTextBoxCell)dgvSub010000_01.Rows[e.RowIndex].Cells[2];//取出卡片內碼
                MessageBox.Show("undone...", cell.Value.ToString(), MessageBoxButtons.OK, MessageBoxIcon.Information);//MessageBox.Show("OK-" + cell.Value.ToString());
            }
        }
        //---人員編輯內頁的卡片列表中增加按鈕，用來實現人員+配卡+授權功能 ~ 增加對應按鈕對應事件
        
        //---
        //增加可改變頁籤顏色功能
        private void m_tabMain_DrawItem(object sender, DrawItemEventArgs e)
        {
            m_tabMain.TabPages[e.Index].ToolTipText = m_tabMain.TabPages[e.Index].Text;//模仿GOOGLE頁籤沒有顯示完整Title時，會用Tip來彌補
            
            Font fntTab;
            Brush bshBack;
            Brush bshFore;
            bool blnUp = false;//把頁籤放在下面必須要動態修正文字的顯示位置旗標，因為系統當設定成自己用程式繪製時，會產生文字在選擇與非選擇的頁籤上的高度差異

            if (e.Index == m_tabMain.SelectedIndex)
            {
                fntTab = new Font(e.Font, FontStyle.Bold);
                bshBack = new System.Drawing.Drawing2D.LinearGradientBrush(e.Bounds, SystemColors.Control, SystemColors.Control, System.Drawing.Drawing2D.LinearGradientMode.BackwardDiagonal);
                bshFore = Brushes.Black;
                //bshBack = new System.Drawing.Drawing2D.LinearGradientBrush(e.Bounds, Color.LightSkyBlue , Color.LightGreen, System.Drawing.Drawing2D.LinearGradientMode.BackwardDiagonal);
                bshBack = new System.Drawing.Drawing2D.LinearGradientBrush(e.Bounds, Color.FromArgb(81, 3, 133), Color.FromArgb(81, 3, 133), System.Drawing.Drawing2D.LinearGradientMode.BackwardDiagonal);
                bshFore = Brushes.White;//Blue;

            }
            else
            {
                fntTab = new Font(e.Font, FontStyle.Bold);//e.Font;
                bshBack = new System.Drawing.Drawing2D.LinearGradientBrush(e.Bounds, SystemColors.Control, SystemColors.Control, System.Drawing.Drawing2D.LinearGradientMode.BackwardDiagonal);//new SolidBrush(SystemColors.Control);
                bshFore =  Brushes.Black;

                bshBack = new System.Drawing.Drawing2D.LinearGradientBrush(e.Bounds, SystemColors.Control, SystemColors.Control, System.Drawing.Drawing2D.LinearGradientMode.BackwardDiagonal);//new SolidBrush(SystemColors.Control);
                bshFore =  Brushes.Black;

                blnUp = true;
            }

            string tabName = m_tabMain.TabPages[e.Index].Text;
            StringFormat sftTab = new StringFormat();
            sftTab.Alignment = StringAlignment.Center;
            e.Graphics.FillRectangle(bshBack, e.Bounds);
            Rectangle recTab = e.Bounds;

            if (blnUp)
            {
                recTab = new Rectangle(recTab.X, recTab.Y + 0, recTab.Width, recTab.Height - 4);
            }
            else
            {
                recTab = new Rectangle(recTab.X, recTab.Y + 4, recTab.Width, recTab.Height - 4);
            }
            
            e.Graphics.DrawString(tabName, fntTab, bshFore, recTab, sftTab);
        }
        //---增加可改變頁籤顏色功能

        //---
        //把所有Outlook主按鈕的切換頁面功能全部設定在系統頁面
        public void TabPage_Push()
        {
            if ( (m_TPOld != null) && ((m_TPOld != m_tabSys) || (m_StackTPOld.Count==0)) )
            {
                m_StackTPOld.Push(m_TPOld);//--2017/02/22 製作返回按鈕功能
            }
        }
        //---把所有Outlook主按鈕的切換頁面功能全部設定在系統頁面

        //---
        //控制器UI多選編輯實作 ~ 撰寫儲存查詢相關變數
        public ArrayList m_ALControllerObj = new ArrayList();
        public int m_intControllerIndex;
        private void initSelectControllerArray()
        {
            m_ALControllerObj.Clear();
            m_intControllerIndex = 0;
            try
            {
                for (int i = (dgvSub0000_01.SelectedRows.Count-1); i >=0 ; i--)
                {
                    int index = dgvSub0000_01.SelectedRows[i].Index;//取得被選取的第一列位置
                    String Strsn = dgvSub0000_01.Rows[index].Cells[4].Value.ToString();
                    m_intdgvSub0000_01_SN = Int32.Parse(Strsn.Replace("unknown-", ""));//控制器列表的unknown列SN也要加上unknown-
                    
                    String SQL = "";
                    bool blncheck = false;
                    SQL = String.Format("SELECT id FROM controller WHERE (state>-1) AND (sn={0})", m_intdgvSub0000_01_SN);
                    MySqlDataReader checkReader = MySQL.GetDataReader(SQL);
                    while (checkReader.Read())
                    {
                        blncheck = true;
                    }
                    checkReader.Close();
                    
                    if (blncheck == true)
                    {
                        m_ALControllerObj.Add(m_intdgvSub0000_01_SN);
                    }
                }
            }
            catch
            {
            }
        }
        //---控制器UI多選編輯實作 ~ 撰寫儲存查詢相關變數        
    }
}
