# MDP.AgentCore

以 AI Agent（Claude Code）開發軟體時使用的 **MDP Skill** 集合：把功能規格流程、架構規範與規範回饋流程寫成 Skill，讓 AI 在每個專案、每次對話都依同一套規則工作。

MDP Skill 一律以 `mdp-{平台}core-{用途}` 命名，例如 `mdp-netcore-architecture`、`mdp-agentcore-specification`。

## Skill 一覽

| Skill | 用途 | 規則文件 |
|---|---|---|
| [mdp-agentcore-specification](skills/mdp-agentcore-specification/) | 功能規格流程（規格驅動開發 SDD）：需求分析 → 系統設計 → 任務拆解 → 程式開發 | [specification.md](skills/mdp-agentcore-specification/references/specification.md) |
| [mdp-netcore-architecture](skills/mdp-netcore-architecture/) | .NET 架構規範：方案與專案分層、依賴方向、程式碼寫法，以及 Context、Entity、Repository、DI、Razor 的設計規則 | [architecture.md](skills/mdp-netcore-architecture/references/architecture.md) |
| [mdp-agentcore-feedback](skills/mdp-agentcore-feedback/) | 規範回饋流程：使用者修正 AI 的產出，或遇到規則沒涵蓋的情況時，判斷是否該成為規則，經使用者確認後寫入 Skill | [feedback.md](skills/mdp-agentcore-feedback/references/feedback.md) |

`mdp-agentcore-specification` 與 `mdp-agentcore-feedback` 也會引用 `mdp-javacore-architecture`、`mdp-vuecore-architecture`、`mdp-pythoncore-architecture`，這幾個 Skill 尚未收錄在本 repository。

## 三個 Skill 如何搭配

```
功能需求
   │
   ▼
mdp-agentcore-specification          ← 決定「做什麼」
   需求分析 ──────────▶ 系統設計 ─────▶ 任務拆解 ─────▶ 程式開發
   01-requirement.md    02-design.md    03-tasks.md     一次一個任務
                           │                              │
                           └──────── 套用 ────────────────┘
                                       │
                                       ▼
                            mdp-{平台}core-architecture   ← 決定「怎麼寫」
                                       │
                 使用者修正 AI 的產出／遇到規則沒涵蓋的情況
                                       │
                                       ▼
                            mdp-agentcore-feedback        ← 提議補進規則，確認後寫入
```

- **功能規格**（`specs/` 底下的文件）只決定做什麼；**架構規範**（`*-architecture` Skill）決定怎麼寫。
- 錯誤修正與不改變商業規則的小幅調整，直接改程式碼，不走功能規格流程。
- 規範只有一個來源：各 Skill 的 `references/*.md`。規範類的修正不存進 AI 的記憶（memory），一律經由回饋流程寫回 Skill。

## 目錄結構

```
MDP.AgentCore/
├── README.md
├── LICENSE
├── skills/
│   └── {Skill}/
│       ├── SKILL.md           ← 給 AI：何時使用、怎麼使用
│       └── references/*.md    ← 給人看的規則全文，規則的唯一來源
└── specs/
    └── 0001-訂單模組/         ← 功能規格的範例
        ├── 01-requirement.md
        ├── 02-design.md
        └── 03-tasks.md
```

## 使用方式

### 1. 安裝 Skill

把需要的 Skill 資料夾複製到專案的 `.claude/skills/` 底下：

```
{專案}/.claude/skills/
├── mdp-agentcore-specification/
├── mdp-agentcore-feedback/
└── mdp-netcore-architecture/
```

### 2. 在專案 `CLAUDE.md` 加上回饋規則

使用者修正 AI 的產出時，通常不會提到「規範」，只靠 Skill 的說明不一定能觸發回饋流程。請在專案 `CLAUDE.md` 的規則段落加上：

```markdown
- 使用者修正你產出的程式碼、設計或規格文件，或你遇到 MDP Skill 規則沒涵蓋的情況時，使用 `mdp-agentcore-feedback` Skill 判斷是否該成為 MDP Skill 的規則；不要把這類修正存進記憶。
```

### 3. 依功能規格流程開發

每個階段建議開新對話，做完一個階段由工程師審核後再進入下一個。

**需求分析**

```
為新功能建立 specs/NNNN-<功能中文名>/01-requirement.md。

功能描述：
<用白話寫：誰、要做什麼、為什麼>

請先閱讀相關的現有程式碼，把會改變的既有行為寫進「對既有行為的影響」。
有疑問的地方列進「待決事項」，不要自行假設。
```

**系統設計**

```
請依 specs/NNNN-<功能中文名>/01-requirement.md 撰寫同資料夾下的 02-design.md。
如果 01-requirement.md 有缺漏或矛盾，先停下來問我，不要自行補上。
```

**任務拆解**

```
請依 specs/NNNN-<功能中文名>/02-design.md 產出 03-tasks.md。
```

**程式開發**

```
依 specs/NNNN-<功能中文名>/03-tasks.md 實作全部任務。
```

完整的流程與文件格式見 [specification.md](skills/mdp-agentcore-specification/references/specification.md)，範例見 [specs/0001-訂單模組](specs/0001-訂單模組/)。

## 修改 Skill

- 規則寫在 `references/*.md`，`SKILL.md` 只說明何時與如何使用規則文件，不重複規則內容。

## 授權

[MIT License](LICENSE)
