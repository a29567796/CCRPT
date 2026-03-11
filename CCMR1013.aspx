<%@ Page Language="C#" AutoEventWireup="true" CodeBehind="CCMR1013.aspx.cs" Inherits="CCRPT.CCMR1013" %>
<!DOCTYPE html>
<html xmlns="http://www.w3.org/1999/xhtml" lang="zh-TW">
<head runat="server">
    <meta http-equiv="Content-Type" content="text/html; charset=utf-8" />
    <meta name="viewport" content="width=device-width, initial-scale=1.0" />
    <title>案件明細 - CCMR1013</title>
    <!-- Bootstrap 4.6 -->
    <link rel="stylesheet"
          href="https://cdn.jsdelivr.net/npm/bootstrap@4.6.2/dist/css/bootstrap.min.css"
          crossorigin="anonymous" />
    <!-- Font Awesome 5 -->
    <link rel="stylesheet"
          href="https://cdnjs.cloudflare.com/ajax/libs/font-awesome/5.15.4/css/all.min.css"
          crossorigin="anonymous" />
    <style type="text/css">
        * { box-sizing: border-box; }
        body {
            background: #e9eef5;
            font-family: 'Microsoft JhengHei', 'PingFang TC', 'Segoe UI', Arial, sans-serif;
            font-size: 13px; color: #333; margin: 0; padding: 0;
        }
        .page-wrapper { max-width: 1120px; margin: 20px auto 40px; padding: 0 12px; }

        /* Title bar */
        .detail-title-bar {
            background: linear-gradient(135deg,#1a3a5c 0%,#2259a0 100%);
            color: #fff; padding: 11px 18px; border-radius: 6px 6px 0 0;
            display: flex; align-items: center; justify-content: space-between;
            box-shadow: 0 2px 4px rgba(0,0,0,.18);
        }
        .detail-title-bar h5 { margin:0; font-size:15px; font-weight:700; letter-spacing:.5px; }
        .btn-close-page {
            background: rgba(255,255,255,.15); color: #fff;
            border: 1px solid rgba(255,255,255,.35); border-radius:4px;
            padding:3px 12px; font-size:13px; cursor:pointer; transition:background .18s; line-height:1.6;
        }
        .btn-close-page:hover { background:rgba(255,255,255,.28); }
        .btn-close-page:focus { outline:none; }

        /* Section cards */
        .sec-card { background:#fff; border:1px solid #d3dae6; margin-bottom:0; border-top:none; }
        .sec-card:first-of-type { border-top:1px solid #d3dae6; }
        .sec-card:last-child { border-radius:0 0 6px 6px; }

        .sec-header {
            background:#f0f5fb; border-bottom:2px solid #2259a0;
            padding:8px 16px; font-weight:700; font-size:13px; color:#1a3a5c;
            display:flex; align-items:center;
        }
        .sec-header .sec-icon { margin-right:7px; color:#2259a0; }
        .sec-header.collapsible { cursor:pointer; user-select:none; }
        .sec-header.collapsible:hover { background:#e4edf8; }
        .sec-header.grey { background:#6c757d; border-bottom:2px solid #5a6268; color:#fff; }
        .sec-header.grey .sec-icon { color:#ddd; }
        .sec-header.grey:hover { background:#636e77; }

        .chevron-icon {
            margin-right:7px; transition:transform .2s;
            display:inline-block; width:12px; flex-shrink:0;
        }
        .chevron-icon.open { transform:rotate(90deg); }
        .sec-body { padding:14px 16px; }
        .sec-body.collapsed { display:none; }

        /* Top info bar */
        .top-info-bar {
            display:flex; flex-wrap:wrap; gap:6px 32px;
            padding:10px 16px; background:#f8fafc; border-bottom:1px solid #d3dae6;
        }
        .tib-item { display:flex; align-items:center; gap:6px; }
        .tib-label { font-weight:700; color:#495057; font-size:12px; white-space:nowrap; }
        .tib-value { color:#1a3a5c; font-weight:700; font-size:13px; }

        /* Field rows */
        .field-row { display:flex; flex-wrap:wrap; align-items:flex-start; margin-bottom:10px; }
        .field-row:last-child { margin-bottom:0; }
        .field-lbl {
            width:140px; min-width:140px; font-weight:700; color:#4a5568;
            padding-top:2px; padding-right:8px; flex-shrink:0; font-size:12px;
        }
        .field-lbl::after { content:':'; }
        .field-val { flex:1; color:#212529; min-width:0; line-height:1.6; }
        .field-val.multiline { white-space:pre-wrap; word-break:break-word; }

        /* File links */
        .file-link {
            display:inline-flex; align-items:center; gap:4px;
            color:#0d6efd; text-decoration:none; font-size:12px;
            margin-right:4px; margin-bottom:3px;
            padding:2px 7px; background:#f0f6ff;
            border:1px solid #c8ddf7; border-radius:4px; transition:background .15s;
        }
        .file-link:hover { background:#daeaff; text-decoration:none; color:#084bb5; }
        .file-list { display:flex; flex-wrap:wrap; gap:3px; }

        /* Data tables */
        .data-tbl { width:100%; border-collapse:collapse; font-size:12px; margin-top:2px; }
        .data-tbl th {
            background:#1a3a5c; color:#fff; padding:7px 10px;
            border:1px solid #0d2845; text-align:left; white-space:nowrap; font-size:12px;
        }
        .data-tbl td { padding:7px 10px; border:1px solid #d3dae6; vertical-align:top; line-height:1.5; }
        .data-tbl tbody tr:nth-child(even) { background:#f7faff; }
        .data-tbl tbody tr:hover { background:#eaf2ff; }

        /* Status badges */
        .badge-status {
            display:inline-block; padding:2px 9px; border-radius:10px;
            font-size:11px; font-weight:600; white-space:nowrap; border:1px solid transparent;
        }
        .bs-going  { background:#fff3cd; color:#856404; border-color:#ffc107; }
        .bs-done   { background:#d1f0dc; color:#155724; border-color:#28a745; }
        .bs-cancel { background:#e2e3e5; color:#383d41; border-color:#adb5bd; }
        .bs-abort  { background:#f8d7da; color:#721c24; border-color:#dc3545; }
        .bs-other  { background:#e8e8e8; color:#555;    border-color:#ccc;    }

        /* Two-column grid */
        .two-col-grid { display:grid; grid-template-columns:1fr 1fr; gap:16px; }
        @media(max-width:640px) { .two-col-grid { grid-template-columns:1fr; } }
        .grid-lbl { font-weight:700; font-size:12px; color:#4a5568; margin-bottom:5px; }
        .grid-lbl::after { content:':'; }
        .grid-val { color:#212529; line-height:1.6; white-space:pre-wrap; word-break:break-word; }

        .no-data-msg { color:#888; font-style:italic; font-size:12px; }
        .mt4 { margin-top:4px; } .mt8 { margin-top:8px; }
        .divider { border:none; border-top:1px solid #e4eaf2; margin:10px 0; }
    </style>
</head>
<body>
<form id="form1" runat="server">
<div class="page-wrapper">

    <%-- Error / notice --%>
    <asp:Literal ID="litError" runat="server" Visible="false"></asp:Literal>

    <%-- ═══════════════════════════════════════════
         Title bar
    ═══════════════════════════════════════════ --%>
    <div class="detail-title-bar">
        <h5><i class="fas fa-clipboard-list mr-2"></i>案件明細</h5>
        <button type="button" class="btn-close-page" onclick="window.close();" title="關閉視窗">
            <i class="fas fa-times mr-1"></i>關閉
        </button>
    </div>

    <%-- ═══════════════════════════════════════════
         Section 1 – Basic info
    ═══════════════════════════════════════════ --%>
    <div class="sec-card" style="border-top:1px solid #d3dae6;">
        <div class="top-info-bar">
            <div class="tib-item">
                <span class="tib-label"><i class="far fa-calendar-alt mr-1"></i>案件日期</span>
                <span class="tib-value"><asp:Literal ID="litCaseDate" runat="server" Text="—"></asp:Literal></span>
            </div>
            <div class="tib-item">
                <span class="tib-label"><i class="fas fa-hashtag mr-1"></i>案件編號</span>
                <span class="tib-value"><asp:Literal ID="litCaseNo" runat="server" Text="—"></asp:Literal></span>
            </div>
        </div>
        <div class="sec-body">
            <div class="field-row">
                <div class="field-lbl">案件主旨</div>
                <div class="field-val"><asp:Literal ID="litSubject" runat="server"></asp:Literal></div>
            </div>
            <div class="field-row">
                <div class="field-lbl">異常說明</div>
                <div class="field-val multiline"><asp:Literal ID="litAnomaly" runat="server"></asp:Literal></div>
            </div>
            <asp:Panel ID="pnlCustInfo" runat="server" Visible="false">
                <hr class="divider" />
                <div class="field-row">
                    <div class="field-lbl">客戶提供資訊</div>
                    <div class="field-val multiline"><asp:Literal ID="litCustInfo" runat="server"></asp:Literal></div>
                </div>
            </asp:Panel>
            <asp:Panel ID="pnlCustFiles" runat="server" Visible="false">
                <div class="field-row mt4">
                    <div class="field-lbl">客戶提供檔案</div>
                    <div class="field-val file-list"><asp:Literal ID="litCustFiles" runat="server"></asp:Literal></div>
                </div>
            </asp:Panel>
        </div>
    </div>

    <%-- ═══════════════════════════════════════════
         Section 2 – 調查報告 及 相關附件
         預設隱藏；有檔案才顯示（展開）
    ═══════════════════════════════════════════ --%>
    <asp:Panel ID="pnlInvestigation" runat="server" Visible="false">
        <div class="sec-card">
            <div class="sec-header collapsible" onclick="toggleSec(this,'invBody')">
                <i class="fas fa-chevron-right chevron-icon open" aria-hidden="true"></i>
                <i class="fas fa-file-alt sec-icon"></i>調查報告 及 相關附件
            </div>
            <div id="invBody" class="sec-body">
                <div class="file-list"><asp:Literal ID="litInvFiles" runat="server"></asp:Literal></div>
            </div>
        </div>
    </asp:Panel>

    <%-- ═══════════════════════════════════════════
         Section 3 – 真因分析
         預設隱藏；有資料才顯示
    ═══════════════════════════════════════════ --%>
    <asp:Panel ID="pnlRootCause" runat="server" Visible="false">
        <div class="sec-card">
            <div class="sec-header">
                <i class="fas fa-search-plus sec-icon"></i>真因分析
            </div>
            <div class="sec-body">
                <div class="field-val multiline"><asp:Literal ID="litRootCause" runat="server"></asp:Literal></div>
            </div>
        </div>
    </asp:Panel>

    <%-- ═══════════════════════════════════════════
         Section 4 – 矯正/預防措施執行暨成效追蹤
         預設隱藏；有 D6/D7 AR 資料才顯示
    ═══════════════════════════════════════════ --%>
    <asp:Panel ID="pnlCorrectiveActions" runat="server" Visible="false">
        <div class="sec-card">
            <div class="sec-header collapsible" onclick="toggleSec(this,'caBody')">
                <i class="fas fa-chevron-right chevron-icon open" aria-hidden="true"></i>
                <i class="fas fa-tools sec-icon"></i>矯正/預防措施執行暨成效追蹤
            </div>
            <div id="caBody" class="sec-body">
                <asp:Literal ID="litCorrectiveActions" runat="server"></asp:Literal>
            </div>
        </div>
    </asp:Panel>

    <%-- ═══════════════════════════════════════════
         Section 5 – Fab fanout  【灰框 1 — 預設收合】
    ═══════════════════════════════════════════ --%>
    <asp:Panel ID="pnlFabFanout" runat="server" Visible="false">
        <div class="sec-card">
            <div class="sec-header grey collapsible" onclick="toggleSec(this,'fabBody')">
                <i class="fas fa-chevron-right chevron-icon" aria-hidden="true"></i>
                <i class="fas fa-network-wired sec-icon"></i>Fab fanout
            </div>
            <div id="fabBody" class="sec-body collapsed">
                <asp:Literal ID="litFabFanout" runat="server"></asp:Literal>
                <asp:Panel ID="pnlFabFiles" runat="server" Visible="false">
                    <div class="mt8">
                        <div class="grid-lbl">附件</div>
                        <div class="file-list mt4"><asp:Literal ID="litFabFiles" runat="server"></asp:Literal></div>
                    </div>
                </asp:Panel>
            </div>
        </div>
    </asp:Panel>

    <%-- ═══════════════════════════════════════════
         Section 6 – 處理時效紀錄  【灰框 2 — 預設收合】
    ═══════════════════════════════════════════ --%>
    <asp:Panel ID="pnlTimeliness" runat="server" Visible="false">
        <div class="sec-card">
            <div class="sec-header grey collapsible" onclick="toggleSec(this,'tlBody')">
                <i class="fas fa-chevron-right chevron-icon" aria-hidden="true"></i>
                <i class="fas fa-clock sec-icon"></i>處理時效紀錄
            </div>
            <div id="tlBody" class="sec-body collapsed">
                <asp:Literal ID="litTimeliness" runat="server"></asp:Literal>
            </div>
        </div>
    </asp:Panel>

    <%-- ═══════════════════════════════════════════
         Section 7 – 客戶回應與意見  【灰框 3 — 預設收合】
    ═══════════════════════════════════════════ --%>
    <asp:Panel ID="pnlCustResponse" runat="server" Visible="false">
        <div class="sec-card" style="border-radius:0 0 6px 6px;">
            <div class="sec-header grey collapsible" onclick="toggleSec(this,'crBody')">
                <i class="fas fa-chevron-right chevron-icon" aria-hidden="true"></i>
                <i class="fas fa-comments sec-icon"></i>客戶回應與意見
            </div>
            <div id="crBody" class="sec-body collapsed">
                <div class="two-col-grid">
                    <div>
                        <div class="grid-lbl">客戶意見</div>
                        <div class="grid-val"><asp:Literal ID="litCustMemo" runat="server"></asp:Literal></div>
                    </div>
                    <div>
                        <div class="grid-lbl">產品處置</div>
                        <div class="grid-val"><asp:Literal ID="litProdDispose" runat="server"></asp:Literal></div>
                    </div>
                </div>
            </div>
        </div>
    </asp:Panel>

</div>
</form>

<script src="https://cdn.jsdelivr.net/npm/jquery@3.6.4/dist/jquery.min.js"
        crossorigin="anonymous"></script>
<script src="https://cdn.jsdelivr.net/npm/bootstrap@4.6.2/dist/js/bootstrap.bundle.min.js"
        crossorigin="anonymous"></script>
<script type="text/javascript">
    /**
     * Toggle a collapsible section.
     * Rotates the chevron and shows/hides the body div.
     */
    function toggleSec(header, bodyId) {
        var body    = document.getElementById(bodyId);
        var chevron = header.querySelector('.chevron-icon');
        if (!body) return;

        var isOpen = !body.classList.contains('collapsed') &&
                     body.style.display !== 'none';

        if (isOpen) {
            body.style.display = 'none';
            body.classList.add('collapsed');
            if (chevron) chevron.classList.remove('open');
        } else {
            body.style.display = '';
            body.classList.remove('collapsed');
            if (chevron) chevron.classList.add('open');
        }
    }
</script>
</body>
</html>
