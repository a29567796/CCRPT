using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.Text;
using System.Web;
using System.Web.UI;
using Oracle.ManagedDataAccess.Client;

namespace CCRPT
{
    /// <summary>
    /// CCMR1013 – 案件明細 WebForm
    ///
    /// 頁面以 QueryString["ccm_no"] 為案件編號，
    /// 從 Oracle 查詢多個資料表後將各區塊渲染至對應 Literal / Panel。
    ///
    /// 顯示/隱藏規則：
    ///   • 客戶提供資訊  ─ 有 content 才顯示
    ///   • 客戶提供檔案  ─ 有檔案才顯示
    ///   • 調查報告附件  ─ 有檔案才顯示（展開）
    ///   • 真因分析      ─ 有 content 才顯示
    ///   • 矯正/預防措施 ─ 有 D6/D7 AR 資料才顯示
    ///   • Fab fanout    ─ 有資料才顯示（灰框 1，預設收合）
    ///   • 處理時效紀錄  ─ 有資料才顯示（灰框 2，預設收合）
    ///   • 客戶回應與意見─ 有資料才顯示（灰框 3，預設收合）
    /// </summary>
    public partial class CCMR1013 : System.Web.UI.Page
    {
        // ─────────────────────────────────────────────────────────────────
        // 資料模型
        // ─────────────────────────────────────────────────────────────────

        private class BasicInfoRow
        {
            public string Header     { get; set; }
            public string ArItems    { get; set; }
            public string Tittle     { get; set; }
            public string MainItem   { get; set; }
            public string MainSeq    { get; set; }
            public string SubItem    { get; set; }
            public string SubSeq     { get; set; }
            public string ArSeq      { get; set; }
            public string Content    { get; set; }
            public string Who        { get; set; }
            public string WhenDate   { get; set; }
            public string Result     { get; set; }
            public string CreateDate { get; set; }
            public string FileName   { get; set; }
            public string FileUrl    { get; set; }
        }

        private class FabFanoutRow
        {
            public string CcmNo                { get; set; }
            public string MainItem             { get; set; }
            public string MainSeq              { get; set; }
            public string ArItem               { get; set; }
            public string ArSeq                { get; set; }
            public string Content              { get; set; }
            public string Standard             { get; set; }
            public string CaseFanIn            { get; set; }
            public string Who                  { get; set; }
            public string FanOutResult         { get; set; }
            public string FanOutWhen           { get; set; }
            public string Evidence             { get; set; }
            public string CloseDate            { get; set; }
            public string FabFanoutFileConnect { get; set; }
        }

        private class TimelinessRow
        {
            public string CcmNo        { get; set; }
            public string Step         { get; set; }
            public string CompleteTime { get; set; }
            public string Remarked     { get; set; }
        }

        private class CustomerResponseRow
        {
            public string CustMemo    { get; set; }
            public string ProdDispose { get; set; }
        }

        private class FileRow
        {
            public string Header               { get; set; }
            public string ArItems              { get; set; }
            public string Step                 { get; set; }
            public string MainItem             { get; set; }
            public string MainSeq              { get; set; }
            public string SubItem              { get; set; }
            public string SubSeq               { get; set; }
            public string FileName             { get; set; }
            public string FileUrl              { get; set; }
            public string FabFanoutFileConnect { get; set; }
        }

        // ─────────────────────────────────────────────────────────────────
        // 連線字串
        // ─────────────────────────────────────────────────────────────────

        private string ConnectionString
        {
            get
            {
                var cs = ConfigurationManager.ConnectionStrings["OracleDB"];
                if (cs == null || string.IsNullOrWhiteSpace(cs.ConnectionString))
                    throw new InvalidOperationException("未在 Web.config 中設定 OracleDB 連線字串。");
                return cs.ConnectionString;
            }
        }

        // ─────────────────────────────────────────────────────────────────
        // Page_Load
        // ─────────────────────────────────────────────────────────────────

        protected void Page_Load(object sender, EventArgs e)
        {
            if (!IsPostBack)
            {
                string ccmNo = (Request.QueryString["ccm_no"] ?? string.Empty).Trim();
                if (!string.IsNullOrEmpty(ccmNo))
                {
                    BindAllData(ccmNo);
                }
                else
                {
                    ShowError("請指定案件編號（URL 參數：ccm_no）。");
                }
            }
        }

        // ─────────────────────────────────────────────────────────────────
        // 資料載入與綁定
        // ─────────────────────────────────────────────────────────────────

        private void BindAllData(string ccmNo)
        {
            try
            {
                var basicRows  = LoadBasicInfo(ccmNo);
                var fanoutRows = LoadFabFanout(ccmNo);
                var tlRows     = LoadTimeliness(ccmNo);
                var custRows   = LoadCustomerResponse(ccmNo);
                var fileRows   = LoadFiles(ccmNo);

                BindBasicInfo(basicRows, fileRows);
                BindInvestigationReport(fileRows);
                BindRootCause(basicRows);
                BindCorrectiveActions(basicRows, fileRows);
                BindFabFanout(fanoutRows, fileRows);
                BindTimeliness(tlRows, fileRows);
                BindCustomerResponse(custRows);

                // Show the more-info toggle bar if at least one grey section has data
                pnlBtnFabFanout.Visible    = pnlFabFanout.Visible;
                pnlBtnTimeliness.Visible   = pnlTimeliness.Visible;
                pnlBtnCustResponse.Visible = pnlCustResponse.Visible;
                pnlMoreInfoBar.Visible     = pnlFabFanout.Visible
                                          || pnlTimeliness.Visible
                                          || pnlCustResponse.Visible;
            }
            catch (Exception ex)
            {
                ShowError("資料載入失敗：" + ex.Message);
            }
        }

        private void BindBasicInfo(List<BasicInfoRow> rows, List<FileRow> fileRows)
        {
            // 案件編號 + 案件日期
            var caseNoRow = rows.Find(r => r.Header == "案件編號");
            if (caseNoRow != null)
            {
                litCaseDate.Text = Enc(caseNoRow.CreateDate);
                litCaseNo.Text   = Enc(caseNoRow.Content);
            }

            // 案件主旨（d2 明細，header = main_item = 案件主旨）
            litSubject.Text = BuildMultiRowContent(rows.FindAll(r => r.Header == "案件主旨"));

            // 異常說明
            litAnomaly.Text = BuildMultiRowContent(rows.FindAll(r => r.Header == "異常說明"));

            // 客戶提供資訊
            string custInfoText = BuildMultiRowContent(rows.FindAll(r => r.Header == "客戶提供資訊"));
            if (!string.IsNullOrWhiteSpace(custInfoText))
            {
                litCustInfo.Text    = custInfoText;
                pnlCustInfo.Visible = true;
            }

            // 客戶提供檔案
            var custFiles = fileRows.FindAll(f => f.Header == "客戶提供檔案");
            if (custFiles.Count > 0)
            {
                litCustFiles.Text    = BuildFileLinks(custFiles);
                pnlCustFiles.Visible = true;
            }
        }

        private void BindInvestigationReport(List<FileRow> fileRows)
        {
            var invFiles = fileRows.FindAll(f => f.Header == "調查報告 及 相關附件");
            if (invFiles.Count > 0)
            {
                litInvFiles.Text         = BuildFileLinks(invFiles);
                pnlInvestigation.Visible = true;
            }
        }

        private void BindRootCause(List<BasicInfoRow> rows)
        {
            string text = BuildMultiRowContent(rows.FindAll(r => r.Header == "真因分析"));
            if (!string.IsNullOrWhiteSpace(text))
            {
                litRootCause.Text    = text;
                pnlRootCause.Visible = true;
            }
        }

        private void BindCorrectiveActions(List<BasicInfoRow> rows, List<FileRow> fileRows)
        {
            var arRows  = rows.FindAll(r => r.Header == "矯正/預防措施執行暨成效追蹤");
            var arFiles = fileRows.FindAll(f => f.Header == "矯正/預防措施執行暨成效追蹤");
            if (arRows.Count > 0)
            {
                litCorrectiveActions.Text        = BuildCorrectiveActionsHtml(arRows, arFiles);
                pnlCorrectiveActions.Visible     = true;
            }
        }

        private void BindFabFanout(List<FabFanoutRow> rows, List<FileRow> fileRows)
        {
            if (rows.Count > 0)
            {
                var fabFiles = fileRows.FindAll(f => f.Header == "Fab fanout");
                litFabFanout.Text    = BuildFabFanoutHtml(rows, fabFiles);
                pnlFabFanout.Visible = true;
            }
        }

        private void BindTimeliness(List<TimelinessRow> rows, List<FileRow> fileRows)
        {
            if (rows.Count > 0)
            {
                var tlFiles = fileRows.FindAll(f => f.Header == "處理時效紀錄");
                litTimeliness.Text    = BuildTimelinessHtml(rows, tlFiles);
                pnlTimeliness.Visible = true;
            }
        }

        private void BindCustomerResponse(List<CustomerResponseRow> rows)
        {
            if (rows.Count > 0)
            {
                litCustResponse.Text    = BuildCustomerResponseHtml(rows);
                pnlCustResponse.Visible = true;
            }
        }

        // ─────────────────────────────────────────────────────────────────
        // HTML 建構器
        // ─────────────────────────────────────────────────────────────────

        /// <summary>矯正/預防措施 表格</summary>
        private string BuildCorrectiveActionsHtml(List<BasicInfoRow> arRows, List<FileRow> arFiles)
        {
            var sb = new StringBuilder();
            sb.Append("<div style='overflow-x:auto'>");
            sb.Append("<table class='data-tbl'><thead><tr>");
            sb.Append("<th style='width:90px'>AR Items</th>");
            sb.Append("<th>執行內容 (What)</th>");
            sb.Append("<th style='width:100px'>負責人 (Who)</th>");
            sb.Append("<th style='width:110px'>完成日期 (When)</th>");
            sb.Append("<th style='width:85px'>狀態 (Result)</th>");
            sb.Append("<th style='width:170px'>附件</th>");
            sb.Append("</tr></thead><tbody>");

            foreach (var row in arRows)
            {
                sb.Append("<tr>");
                sb.AppendFormat("<td><strong>{0}</strong></td>", Enc(row.ArItems));
                sb.AppendFormat("<td style='white-space:pre-wrap;word-break:break-word'>{0}</td>",
                                Enc(row.Content));
                sb.AppendFormat("<td>{0}</td>", Enc(row.Who));
                sb.AppendFormat("<td>{0}</td>", Enc(row.WhenDate));
                sb.AppendFormat("<td>{0}</td>", GetResultBadge(row.Result));

                // 附件：依 ar_items 匹配
                var files = arFiles.FindAll(f => f.ArItems == row.ArItems);
                sb.Append("<td class='file-list'>");
                foreach (var f in files)
                    AppendFileLink(sb, f.FileUrl, f.FileName);
                sb.Append("</td>");
                sb.Append("</tr>");
            }

            sb.Append("</tbody></table></div>");
            return sb.ToString();
        }

        /// <summary>Fab fanout 表格（不含主項目欄，附件依 fabfanout_fileconnect 精準配對）</summary>
        private string BuildFabFanoutHtml(List<FabFanoutRow> rows, List<FileRow> fabFiles)
        {
            var sb = new StringBuilder();
            sb.Append("<div style='overflow-x:auto'>");
            sb.Append("<table class='data-tbl'><thead><tr>");
            sb.Append("<th>項目內容</th>");
            sb.Append("<th style='width:90px'>Case-Fan-In(Y/N)</th>");
            sb.Append("<th style='width:110px'>Who</th>");
            sb.Append("<th style='width:100px'>Result</th>");
            sb.Append("<th style='width:110px'>When</th>");
            sb.Append("<th>Evidence</th>");
            sb.Append("<th style='width:110px'>Close Date</th>");
            sb.Append("<th style='width:170px'>附件</th>");
            sb.Append("</tr></thead><tbody>");

            foreach (var row in rows)
            {
                sb.Append("<tr>");
                sb.AppendFormat("<td style='white-space:pre-wrap;word-break:break-word'>{0}</td>",
                                Enc(row.Content));
                sb.AppendFormat("<td style='text-align:center'>{0}</td>", Enc(row.CaseFanIn));
                sb.AppendFormat("<td>{0}</td>", Enc(row.Who));
                sb.AppendFormat("<td>{0}</td>", Enc(row.FanOutResult));
                sb.AppendFormat("<td>{0}</td>", Enc(row.FanOutWhen));
                sb.AppendFormat("<td style='white-space:pre-wrap;word-break:break-word'>{0}</td>",
                                Enc(row.Evidence));
                sb.AppendFormat("<td>{0}</td>", Enc(row.CloseDate));

                // 附件：依 fabfanout_fileconnect 精準配對（兩端均不為空才比對）
                var matched = fabFiles.FindAll(f => !string.IsNullOrEmpty(row.FabFanoutFileConnect)
                                                 && !string.IsNullOrEmpty(f.FabFanoutFileConnect)
                                                 && f.FabFanoutFileConnect == row.FabFanoutFileConnect);
                sb.Append("<td class='file-list'>");
                foreach (var f in matched)
                    AppendFileLink(sb, f.FileUrl, f.FileName);
                sb.Append("</td>");
                sb.Append("</tr>");
            }

            sb.Append("</tbody></table></div>");
            return sb.ToString();
        }

        /// <summary>處理時效紀錄 表格</summary>
        private string BuildTimelinessHtml(List<TimelinessRow> rows, List<FileRow> tlFiles)
        {
            var sb = new StringBuilder();
            sb.Append("<div style='overflow-x:auto'>");
            sb.Append("<table class='data-tbl'><thead><tr>");
            sb.Append("<th style='width:270px'>Step</th>");
            sb.Append("<th style='width:160px'>完成日期</th>");
            sb.Append("<th>備註 (Remark)</th>");
            sb.Append("<th style='width:170px'>附件</th>");
            sb.Append("</tr></thead><tbody>");

            foreach (var row in rows)
            {
                sb.Append("<tr>");
                sb.AppendFormat("<td>{0}</td>", Enc(row.Step));
                sb.AppendFormat("<td>{0}</td>", Enc(row.CompleteTime));
                sb.AppendFormat("<td style='white-space:pre-wrap;word-break:break-word'>{0}</td>",
                                Enc(row.Remarked));

                // 附件：依 step 匹配
                var files = tlFiles.FindAll(f => f.Step == row.Step);
                sb.Append("<td class='file-list'>");
                foreach (var f in files)
                    AppendFileLink(sb, f.FileUrl, f.FileName);
                sb.Append("</td>");
                sb.Append("</tr>");
            }

            sb.Append("</tbody></table></div>");
            return sb.ToString();
        }

        /// <summary>客戶回應與意見 三欄表格（客戶意見、產品處置、時間）</summary>
        private string BuildCustomerResponseHtml(List<CustomerResponseRow> rows)
        {
            var sb = new StringBuilder();
            sb.Append("<div style='overflow-x:auto'>");
            sb.Append("<table class='data-tbl'><thead><tr>");
            sb.Append("<th>客戶意見</th>");
            sb.Append("<th style='width:200px'>產品處置</th>");
            sb.Append("<th style='width:140px'>時間</th>");
            sb.Append("</tr></thead><tbody>");

            foreach (var row in rows)
            {
                string memo = string.IsNullOrWhiteSpace(row.CustMemo) ? "無" : row.CustMemo;
                sb.Append("<tr>");
                sb.AppendFormat("<td style='white-space:pre-wrap;word-break:break-word'>{0}</td>",
                                FmtMultiline(memo));
                sb.AppendFormat("<td>{0}</td>", Enc(MapProdDispose(row.ProdDispose)));
                sb.Append("<td>—</td>");
                sb.Append("</tr>");
            }

            sb.Append("</tbody></table></div>");
            return sb.ToString();
        }

        /// <summary>產生檔案連結清單 HTML</summary>
        private string BuildFileLinks(List<FileRow> files)
        {
            var sb = new StringBuilder();
            foreach (var f in files)
                AppendFileLink(sb, f.FileUrl, f.FileName);
            return sb.ToString();
        }

        private void AppendFileLink(StringBuilder sb, string url, string name)
        {
            if (string.IsNullOrWhiteSpace(url) && string.IsNullOrWhiteSpace(name))
                return;

            string displayName = string.IsNullOrWhiteSpace(name) ? url : name;
            if (!string.IsNullOrWhiteSpace(url))
            {
                sb.AppendFormat(
                    "<a href='{0}' target='_blank' class='file-link'>" +
                    "<i class='fas fa-paperclip' aria-hidden='true'></i>{1}</a>",
                    Enc(url), Enc(displayName));
            }
            else
            {
                sb.AppendFormat(
                    "<span class='no-data-msg'><i class='fas fa-paperclip' aria-hidden='true'></i>" +
                    "{0}</span>", Enc(displayName));
            }
        }

        /// <summary>合併多列 content 欄位（保留換行排版）</summary>
        private string BuildMultiRowContent(List<BasicInfoRow> rows)
        {
            if (rows == null || rows.Count == 0) return string.Empty;
            var sb = new StringBuilder();
            foreach (var row in rows)
            {
                if (!string.IsNullOrWhiteSpace(row.Content))
                    sb.Append(FmtMultiline(row.Content));
            }
            return sb.ToString();
        }

        // ─────────────────────────────────────────────────────────────────
        // 資料查詢
        // ─────────────────────────────────────────────────────────────────

        private List<BasicInfoRow> LoadBasicInfo(string ccmNo)
        {
            // 原始 SQL 保留不動（含 UNION），以 WHENDATE 別名避免保留字問題
            const string sql = @"
SELECT DISTINCT
       '案件編號' AS HEADER, '' AS AR_ITEMS, 'd2' AS TITTLE,
       '' AS MAIN_ITEM, '1' AS MAIN_SEQ, '' AS SUB_ITEM,
       '' AS SUB_SEQ, '' AS AR_SEQ, ccm_no AS CONTENT,
       '' AS WHO, '' AS WHENDATE, '' AS RESULT,
       TO_CHAR(case_date, 'YYYY/MM/DD') AS CREATE_DATE,
       '' AS FILE_NAME, '' AS FILE_URL
  FROM ccm_8dreport
 WHERE ccm_no = :ccm_no
UNION
SELECT cd.main_item AS HEADER, '' AS AR_ITEMS, cd.tittle AS TITTLE,
       cd.main_item AS MAIN_ITEM, cd.main_seq AS MAIN_SEQ,
       cd.sub_item AS SUB_ITEM, cd.sub_seq AS SUB_SEQ, cd.ar_seq AS AR_SEQ,
       cd.content AS CONTENT, cd.who AS WHO,
       TO_CHAR(cd.""when"", 'YYYY/MM/DD') AS WHENDATE,
       DECODE(cd.result,'going','進行中','done','已完成',
              'cancel','取消','abrogation','終止','') AS RESULT,
       TO_CHAR(cd.create_date, 'YYYY/MM/DD') AS CREATE_DATE,
       '' AS FILE_NAME, '' AS FILE_URL
  FROM ccm_8dreport_detail cd
 WHERE cd.ccm_no = :ccm_no
   AND cd.tittle IN ('d2')
UNION
SELECT DECODE(sub_item,'真因','真因分析','矯正/預防措施執行暨成效追蹤') AS HEADER,
       DECODE(sub_item,'D6AR','D6-'||ar_seq,'D7AR','D7-'||ar_seq,'') AS AR_ITEMS,
       cd.tittle AS TITTLE, cd.main_item AS MAIN_ITEM, cd.main_seq AS MAIN_SEQ,
       cd.sub_item AS SUB_ITEM, cd.sub_seq AS SUB_SEQ, cd.ar_seq AS AR_SEQ,
       cd.content AS CONTENT, cd.who AS WHO,
       TO_CHAR(cd.""when"", 'YYYY/MM/DD') AS WHENDATE,
       DECODE(cd.result,'going','進行中','done','已完成',
              'cancel','取消','abrogation','終止','') AS RESULT,
       TO_CHAR(cd.create_date, 'YYYY/MM/DD') AS CREATE_DATE,
       '' AS FILE_NAME, '' AS FILE_URL
  FROM ccm_8dreport_detail cd
 WHERE cd.ccm_no = :ccm_no
   AND main_seq = DECODE(tittle, 'd4d6',
                    (SELECT MAX(main_seq) FROM ccm_8dreport_detail
                      WHERE tittle = 'd4d6' AND ccm_no = cd.ccm_no), 1)
   AND cd.tittle IN ('d4d6','d7')
   AND sub_item IN ('真因','D6AR','D7AR')";

            return Query(sql, ccmNo, dr => new BasicInfoRow
            {
                Header     = S(dr, "HEADER"),
                ArItems    = S(dr, "AR_ITEMS"),
                Tittle     = S(dr, "TITTLE"),
                MainItem   = S(dr, "MAIN_ITEM"),
                MainSeq    = S(dr, "MAIN_SEQ"),
                SubItem    = S(dr, "SUB_ITEM"),
                SubSeq     = S(dr, "SUB_SEQ"),
                ArSeq      = S(dr, "AR_SEQ"),
                Content    = S(dr, "CONTENT"),
                Who        = S(dr, "WHO"),
                WhenDate   = S(dr, "WHENDATE"),
                Result     = S(dr, "RESULT"),
                CreateDate = S(dr, "CREATE_DATE"),
                FileName   = S(dr, "FILE_NAME"),
                FileUrl    = S(dr, "FILE_URL"),
            });
        }

        private List<FabFanoutRow> LoadFabFanout(string ccmNo)
        {
            const string sql = @"
SELECT a.ccm_no AS CCM_NO, a.main_item AS MAIN_ITEM,
       TO_CHAR(a.main_seq) AS MAIN_SEQ, a.ar_item AS AR_ITEM,
       TO_CHAR(a.ar_seq) AS AR_SEQ, a.content AS CONTENT,
       a.standard AS STANDARD, a.case_fan_in AS CASE_FAN_IN,
       (a.who || a.fan_out_who) AS WHO,
       a.fan_out_result AS FAN_OUT_RESULT,
       TO_CHAR(a.fan_out_when, 'YYYY/MM/DD') AS FAN_OUT_WHEN,
       a.evidence AS EVIDENCE,
       TO_CHAR(a.close_date, 'YYYY/MM/DD') AS CLOSE_DATE,
       (NVL(a.ar_item,'') || '-' || NVL(TO_CHAR(a.ar_seq),'')) AS FABFANOUT_FILECONNECT
  FROM ccm_8dreport_qa_follow a
 WHERE a.ccm_no = :ccm_no
   AND a.fan_out_flag = 'Y'
   AND a.main_item <> '內部調查(總結)'
   AND a.main_seq = (SELECT MAX(b.main_seq)
                       FROM ccm_8dreport_qa_follow b
                      WHERE b.fan_out_flag = 'Y'
                        AND b.main_item <> '內部調查(總結)'
                        AND b.ccm_no = a.ccm_no)";

            return Query(sql, ccmNo, dr => new FabFanoutRow
            {
                CcmNo                = S(dr, "CCM_NO"),
                MainItem             = S(dr, "MAIN_ITEM"),
                MainSeq              = S(dr, "MAIN_SEQ"),
                ArItem               = S(dr, "AR_ITEM"),
                ArSeq                = S(dr, "AR_SEQ"),
                Content              = S(dr, "CONTENT"),
                Standard             = S(dr, "STANDARD"),
                CaseFanIn            = S(dr, "CASE_FAN_IN"),
                Who                  = S(dr, "WHO"),
                FanOutResult         = S(dr, "FAN_OUT_RESULT"),
                FanOutWhen           = S(dr, "FAN_OUT_WHEN"),
                Evidence             = S(dr, "EVIDENCE"),
                CloseDate            = S(dr, "CLOSE_DATE"),
                FabFanoutFileConnect = S(dr, "FABFANOUT_FILECONNECT"),
            });
        }

        private List<TimelinessRow> LoadTimeliness(string ccmNo)
        {
            const string sql = @"
SELECT a.ccm_no AS CCM_NO,
       DECODE(a.main_item,
              'caseDate', '客戶Issue Date',
              'd3',       'D3(一個工作天內完成)',
              'd8',       'D8(10個工作天內完成or依客戶要求)',
              a.main_item) AS STEP,
       TO_CHAR(a.record_time, 'YYYY/MM/DD HH24:MI') AS COMPLETE_TIME,
       a.content AS REMARKED
  FROM ccm_8dreport_timeliness a
 WHERE a.ccm_no = :ccm_no
 ORDER BY DECODE(a.main_item, 'caseDate', 1, 'd3', 2, 'd8', 3, 9)";

            return Query(sql, ccmNo, dr => new TimelinessRow
            {
                CcmNo        = S(dr, "CCM_NO"),
                Step         = S(dr, "STEP"),
                CompleteTime = S(dr, "COMPLETE_TIME"),
                Remarked     = S(dr, "REMARKED"),
            });
        }

        private List<CustomerResponseRow> LoadCustomerResponse(string ccmNo)
        {
            // 原始資料表無標準時間欄位；時間欄位在 UI 中顯示 — 作為預留位置
            const string sql = @"
SELECT NVL(a.cust_memo, '無') AS CUST_MEMO,
       a.prod_dispose AS PROD_DISPOSE
  FROM ccm_8dreport_se_cust a
 WHERE a.ccm_no = :ccm_no";

            return Query(sql, ccmNo, dr => new CustomerResponseRow
            {
                CustMemo    = S(dr, "CUST_MEMO"),
                ProdDispose = S(dr, "PROD_DISPOSE"),
            });
        }

        private List<FileRow> LoadFiles(string ccmNo)
        {
            // 路徑替換值從 Web.config appSettings 讀取，避免硬編碼內部網路資訊
            string fileShareRoot = ConfigurationManager.AppSettings["FileShareRoot"]
                                   ?? @"\\snas1\Server_Share\ECS\程式拋轉區\CCM\";
            string fileUrlBase   = ConfigurationManager.AppSettings["FileUrlBase"]
                                   ?? "openedge:10.5.0.54/CCM_FILE/";

            // REPLACE 中的路徑由 C# 端替換，SQL 使用參數化佔位符
            string sql = @"
SELECT DISTINCT
       DECODE(main_item,
              'd2',          '客戶提供檔案',
              'd4qa',        '調查報告 及 相關附件',
              'd6qa',        '矯正/預防措施執行暨成效追蹤',
              'd7qa',        '矯正/預防措施執行暨成效追蹤',
              'QA提供客戶',  'Fab fanout',
              'timeliness1', '處理時效紀錄',
              'timeliness2', '處理時效紀錄',
              'timeliness3', '處理時效紀錄',
              '') AS HEADER,
       DECODE(main_item,
              'd6qa', 'D6-' || sub_seq,
              'd7qa', 'D7-' || sub_seq,
              '') AS AR_ITEMS,
       DECODE(main_item,
              'timeliness1', '客戶Issue Date',
              'timeliness2', 'D3(一個工作天內完成)',
              'timeliness3', 'D8(10個工作天內完成or依客戶要求)',
              '') AS STEP,
       main_item AS MAIN_ITEM,
       NVL(TO_CHAR(main_seq), '') AS MAIN_SEQ,
       sub_item AS SUB_ITEM,
       NVL(TO_CHAR(sub_seq), '') AS SUB_SEQ,
       file_name AS FILE_NAME,
       REPLACE(file_link, :share_root, :url_base) AS FILE_URL,
       (NVL(cf.sub_item,'') || '-' || NVL(TO_CHAR(cf.sub_seq),'')) AS FABFANOUT_FILECONNECT
  FROM ccm_8dreport_file cf
 WHERE cf.ccm_no = :ccm_no
   AND ( (main_item IN ('d2','d4qa','d6qa','d7qa',
                        'timeliness1','timeliness2','timeliness3')
          AND NVL(TO_CHAR(main_seq), '1') =
              DECODE(main_item,
                     'd4qa', (SELECT TO_CHAR(MAX(main_seq))
                                FROM ccm_8dreport_detail
                               WHERE tittle = 'd4d6' AND ccm_no = cf.ccm_no),
                     'd6qa', (SELECT TO_CHAR(MAX(main_seq))
                                FROM ccm_8dreport_detail
                               WHERE tittle = 'd4d6' AND ccm_no = cf.ccm_no),
                     'd7qa', (SELECT TO_CHAR(MAX(main_seq))
                                FROM ccm_8dreport_detail
                               WHERE tittle = 'd7'   AND ccm_no = cf.ccm_no),
                     '1'))
     OR (fan_in_item = 'fanout' AND sub_item IN ('D6AR','D7AR'))
     OR main_item = 'QA提供客戶' )
 ORDER BY main_seq, sub_seq";

            return QueryWithParams(sql, dr => new FileRow
            {
                Header               = S(dr, "HEADER"),
                ArItems              = S(dr, "AR_ITEMS"),
                Step                 = S(dr, "STEP"),
                MainItem             = S(dr, "MAIN_ITEM"),
                MainSeq              = S(dr, "MAIN_SEQ"),
                SubItem              = S(dr, "SUB_ITEM"),
                SubSeq               = S(dr, "SUB_SEQ"),
                FileName             = S(dr, "FILE_NAME"),
                FileUrl              = S(dr, "FILE_URL"),
                FabFanoutFileConnect = S(dr, "FABFANOUT_FILECONNECT"),
            }, new OracleParameter(":ccm_no",    OracleDbType.Varchar2) { Value = ccmNo },
               new OracleParameter(":share_root", OracleDbType.Varchar2) { Value = fileShareRoot },
               new OracleParameter(":url_base",   OracleDbType.Varchar2) { Value = fileUrlBase });
        }

        // ─────────────────────────────────────────────────────────────────
        // 共用查詢執行器
        // ─────────────────────────────────────────────────────────────────

        /// <summary>
        /// 執行 Oracle 查詢（單一 :ccm_no 參數），以 DataTable 回傳並 map 至 T。
        /// BindByName = true，UNION 中重複的同名參數只需加一次。
        /// </summary>
        private List<T> Query<T>(string sql, string ccmNo, Func<DataRow, T> mapper)
        {
            return QueryWithParams(sql, mapper,
                new OracleParameter(":ccm_no", OracleDbType.Varchar2) { Value = ccmNo });
        }

        /// <summary>執行 Oracle 查詢（任意參數集合），以 DataTable 回傳並 map 至 T。</summary>
        private List<T> QueryWithParams<T>(string sql, Func<DataRow, T> mapper,
                                           params OracleParameter[] parameters)
        {
            var list = new List<T>();
            var dt   = new DataTable();

            using (var conn = new OracleConnection(ConnectionString))
            using (var cmd  = new OracleCommand(sql, conn))
            {
                cmd.BindByName = true;
                foreach (var p in parameters)
                    cmd.Parameters.Add(p);

                conn.Open();
                using (var adapter = new OracleDataAdapter(cmd))
                    adapter.Fill(dt);
            }

            foreach (DataRow dr in dt.Rows)
                list.Add(mapper(dr));

            return list;
        }

        // ─────────────────────────────────────────────────────────────────
        // 輔助方法
        // ─────────────────────────────────────────────────────────────────

        /// <summary>安全讀取 DataRow 字串欄位（大小寫容錯）</summary>
        private static string S(DataRow dr, string col)
        {
            if (dr.Table.Columns.Contains(col))
                return dr.IsNull(col) ? string.Empty : dr[col].ToString();
            // 嘗試小寫
            string lower = col.ToLowerInvariant();
            if (dr.Table.Columns.Contains(lower))
                return dr.IsNull(lower) ? string.Empty : dr[lower].ToString();
            return string.Empty;
        }

        /// <summary>HTML 編碼</summary>
        private static string Enc(string s) =>
            HttpUtility.HtmlEncode(s ?? string.Empty);

        /// <summary>多行文字轉 HTML（保留換行，防 XSS）</summary>
        private static string FmtMultiline(string s)
        {
            if (string.IsNullOrEmpty(s)) return string.Empty;
            return HttpUtility.HtmlEncode(s)
                              .Replace("\r\n", "\n")
                              .Replace("\r",   "\n")
                              .Replace("\n",   "<br />");
        }

        /// <summary>狀態值轉 Badge HTML</summary>
        private static string GetResultBadge(string result)
        {
            if (string.IsNullOrWhiteSpace(result))
                return string.Empty;

            string css;
            switch (result)
            {
                case "進行中": css = "bs-going";  break;
                case "已完成": css = "bs-done";   break;
                case "取消":   css = "bs-cancel"; break;
                case "終止":   css = "bs-abort";  break;
                default:       css = "bs-other";  break;
            }
            return $"<span class='badge-status {css}'>{Enc(result)}</span>";
        }

        /// <summary>
        /// prod_dispose 轉顯示文字：
        ///   選程1 → 退貨至成品倉
        ///   選程2 → 退貨至廢品倉
        ///   其他  → 顯示欄位本身內容（適用選程3 使用者自填）
        /// </summary>
        private static string MapProdDispose(string prodDispose)
        {
            switch ((prodDispose ?? string.Empty).Trim())
            {
                case "選程1": return "退貨至成品倉";
                case "選程2": return "退貨至廢品倉";
                default:      return prodDispose ?? string.Empty;
            }
        }

        /// <summary>顯示錯誤訊息（msg 為純文字，內部自動 HTML 編碼）</summary>
        private void ShowError(string msg)
        {
            litError.Text =
                "<div style='background:#fff3cd;border:1px solid #ffc107;border-radius:4px;" +
                "padding:10px 14px;margin-bottom:10px;font-size:13px;color:#856404;'>" +
                "<i class='fas fa-exclamation-triangle mr-2'></i>" +
                HttpUtility.HtmlEncode(msg) + "</div>";
            litError.Visible = true;
        }
    }
}
