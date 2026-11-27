# 🏆 QuizMon – Монгол хэлний онлайн асуулт тоглоом

Kahoot-той төстэй, Firebase дээр ажилладаг, Vercel-д байршуулагдах **асуулт уралдааны систем**.

---

## ✨ Боломжууд
| Боломж | Дэлгэрэнгүй |
|---|---|
| 👨‍🏫 Багшийн бүртгэл | Firebase Auth имэйл/нууц үг |
| 📝 Асуулт үүсгэх | 10 / 15 / 20 / 25 асуулт, 4 сонголттой |
| 📱 QR код | Тоглоом бүрт автоматаар үүснэ |
| 👥 Хамгийн ихдээ 40 сурагч | Тоглоом дүүрсэн бол нэгдэхгүй |
| ⏱ 60 секундын таймер | Хариулт → үлдсэн секунд = оноо |
| 🏆 Ялагчийн самбар | Эцсийн дүн, медалтай |

---

## 🚀 Суулгалт ба ажиллуулалт

### 1. Firebase тохиргоо

1. [Firebase Console](https://console.firebase.google.com/) → "Add project" дарна
2. **Authentication** → Email/Password идэвхжүүлнэ
3. **Firestore Database** → "Create database" (test mode)
4. **Project Settings** → "Your apps" → Web app бүртгэнэ
5. Config-оо хуулж `js/config.js`-д оруулна:

```js
const firebaseConfig = {
  apiKey:            "AIzaSy...",
  authDomain:        "your-project.firebaseapp.com",
  projectId:         "your-project-id",
  storageBucket:     "your-project.appspot.com",
  messagingSenderId: "123456789",
  appId:             "1:123:web:abc"
};
```

#### Firestore Security Rules (хөгжүүлэлтэд)
Firebase Console → Firestore → Rules tab → дараахыг оруулна:
```
rules_version = '2';
service cloud.firestore {
  match /databases/{database}/documents {
    match /{document=**} {
      allow read, write: if true;
    }
  }
}
```

---

### 2. Git + GitHub

```bash
cd QuizMon
git init
git add .
git commit -m "🎉 Initial QuizMon commit"
git remote add origin https://github.com/YOUR_USERNAME/quizmon.git
git push -u origin main
```

---

### 3. Vercel байршуулалт

1. [vercel.com](https://vercel.com) → "Add New Project"
2. GitHub repo холбоно
3. Framework: **Other** (Static Site)
4. Root directory: `QuizMon`
5. Deploy!

---

## 📁 Файлын бүтэц

```
QuizMon/
├── index.html        ← Нэвтрэх / бүртгэх (багш)
├── dashboard.html    ← Багшийн хяналтын самбар
├── editor.html       ← Асуулт үүсгэх / засах
├── host.html         ← Тоглоом удирдах (багш)
├── join.html         ← Сурагч нэгдэх (QR кодны буух хуудас)
├── play.html         ← Сурагчийн тоглоомын хуудас
├── css/
│   └── main.css      ← Дизайн
├── js/
│   └── config.js     ← Firebase тохиргоо ← ЭНД ОРУУЛНА!
└── vercel.json       ← Vercel тохиргоо
```

---

## 🎮 Тоглоомын дүрэм

1. **Багш** → Dashboard → Асуулт сонгоно → ▶ Эхлүүлэх
2. **QR код** гарна → сурагчид утсаараа уншина
3. Сурагч: **нэр + аватар** сонгоод нэгдэнэ
4. Багш "Тоглоом эхлүүлэх" дарна
5. **60 секунд** бүр асуулт гарна
6. **Хурдан зөв хариулсан** сурагч илүү оноо авна
   - 1 секундэд хариулсан → 59 оноо
   - 30 секундэд хариулсан → 30 оноо
   - Буруу хариулт → 0 оноо
7. Бүх асуулт дууссаны дараа **🏆 Ялагчийн самбар** гарна

---

## 🛠 Хөгжүүлэлт

Local-д нээхдээ Live Server extension ашиглана (VS Code):
1. VS Code → Extensions → "Live Server" суулгана
2. `index.html` дээр хулгана хулгана хулгана → "Open with Live Server"

---

*QuizMon © 2026 – Монгол сурагчдад зориулав* 🇲🇳
