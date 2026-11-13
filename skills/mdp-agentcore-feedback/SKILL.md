---
name: mdp-agentcore-feedback
description: MDP.AgentCore 的規範回饋流程：使用者修正了你產出的程式碼、設計或規格文件，或你依任何 MDP Skill（以 mdp-{平台}core-{用途} 命名，例如 mdp-netcore-architecture、mdp-agentcore-specification，包括本 Skill）工作時遇到規則沒涵蓋的情況，判斷是否該成為 MDP Skill 的規則，提議寫入位置與規則原文，等使用者確認後才修改該 Skill 的 references 文件。只要使用者指出你的產出有錯、要求改寫法、否決你的做法並說明理由、自己動手改了你的產出，或你必須依規則的理由自行判斷規則沒寫到的情況，都要使用這個 Skill；規範類的修正不要存進記憶（memory）。
---

# MDP.AgentCore 規範回饋

流程全文在 [references/feedback.md](references/feedback.md)，這是給人看的文件，也是規則的唯一來源。

## 使用方式

1. **先完成手上的工作**：使用者修正時，照修正改好；遇到規則沒涵蓋的情況時，依相關規則的理由判斷怎麼做，並在回覆中說明（第 1 章）。
2. **讀 references/feedback.md**，依第 2 章找到對應的 Skill 與規則文件，讀相關章節。
3. **依第 3 章判斷**：新增、修改、改寫措辭、寫進專案 `CLAUDE.md`，或不提議。
4. **需要提議時，依第 4 章的格式**放在回覆最後。
5. **使用者明確同意後，依第 5 章寫入**，規則不是一看就懂時才附上理由。

規範類的修正不存進記憶（memory），一律走這個流程。
