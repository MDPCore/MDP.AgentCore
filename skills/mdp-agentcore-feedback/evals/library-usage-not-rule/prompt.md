---
tags: [skill-creator]
max_turns: 30
timeout_seconds: 900
allowed_tools: [Read, Glob, Grep, Skill, Write, Edit]
---

Orders 模組的新增訂單頁，交貨日期的日曆上所有日期都不能選了。原因是你把日曆元件的 disableDayFn 參數拿掉，元件沒收到這個參數時預設會停用所有日期。請改成傳入一律回傳 false 的 disableDayFn。
