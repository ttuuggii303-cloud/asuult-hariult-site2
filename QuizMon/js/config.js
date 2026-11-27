// ============================================================
// QuizMon – Firebase Configuration
// 1. Firebase Console → Project Settings → Your Apps → Web App
// 2. Copy the config object below and replace the placeholders
// ============================================================
const firebaseConfig = {
  apiKey:            "YOUR_API_KEY",
  authDomain:        "YOUR_PROJECT_ID.firebaseapp.com",
  projectId:         "YOUR_PROJECT_ID",
  storageBucket:     "YOUR_PROJECT_ID.appspot.com",
  messagingSenderId: "YOUR_MESSAGING_SENDER_ID",
  appId:             "YOUR_APP_ID"
};

firebase.initializeApp(firebaseConfig);

// Shortcuts used across all pages
const auth = firebase.auth();
const db   = firebase.firestore();
