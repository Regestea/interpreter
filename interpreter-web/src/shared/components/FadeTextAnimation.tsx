import React, { useState } from "react";
import { motion } from "framer-motion";

const FadeTextAnimation: React.FC = () => {
    const [currentText, setCurrentText] = useState("Original Text"); // متنی که نمایش داده می‌شود
    const [pendingText, setPendingText] = useState<string | null>(null); // متنی که منتظر تیره شدن است

    // تابعی که با کلیک دکمه فقط متن را رزرو می‌کند
    const handleButtonClick = () => {
        setPendingText("Updated Content! 🚀");
    };

    return (
        <div style={{ textAlign: "center", marginTop: "50px" }}>

            {/* کامپوننت انیمیشن */}
            <motion.div
                animate={{ opacity: [1, 0, 1] }}
                transition={{
                    duration: 3,
                    ease: "easeInOut",
                    repeat: Infinity,
                }}
                onUpdate={(latest) => {
                    // بررسی می‌کنیم: اگر opacity نزدیک به صفر بود و متنی در نوبت انتظار بود
                    if (pendingText && latest.opacity !== undefined && (latest.opacity as number) < 0.02) {
                        setCurrentText(pendingText); // تغییر متن اصلی
                        setPendingText(null); // خالی کردن نوبت انتظار
                    }
                }}
                style={{ fontSize: "2rem", fontWeight: "bold", height: "60px" }}
            >
                {currentText}
            </motion.div>

            <button
                onClick={handleButtonClick}
                style={{
                    marginTop: "20px",
                    padding: "10px 20px",
                    cursor: "pointer",
                    borderRadius: "8px",
                    border: "none",
                    backgroundColor: "#007bff",
                    color: "white"
                }}
            >
                درخواست تغییر متن
            </button>
        </div>
    );
};

export default FadeTextAnimation;
