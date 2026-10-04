# اصلاحات 0.8.7 — نتیجهٔ ۹ مورد

1. Core execution security: هش اولیهٔ exe و DLLهای هسته داخل برنامه embed شده است؛ قبل از تمام اجراها، حتی version و validate، بررسی می‌شود. فایل‌ها از پیش از hash تا خروج Process با FileShare.Read قفل‌اند. هستهٔ دانلودشده با digest رسمی GitHub و سپس hash فایل‌های داخل ZIP تأیید می‌شود؛ هش نسخهٔ نصب‌شده در HKLM ذخیره می‌شود تا پردازش کاربر بدون ادمین نتواند آن را تغییر دهد. manifest فعلاً requireAdministrator است. این بررسی برای فایل‌های Core است و امضای Authenticode کل برنامه نیست.

2. Session secret cleanup: پوشه‌های GUID باقی‌مانده در Startup پاک می‌شوند؛ خطای حذف فایل قفل‌شده کرش نمی‌دهد. JSON اتصال پس از آماده‌شدن هسته حذف می‌شود. فایل ویرایش/تست نیز زیر sessions قرار می‌گیرد تا پس از crash قابل پاک‌سازی باشد. Disconnect پاک‌سازی را حفظ می‌کند. پاک‌سازی پس از Crash هنگام اجرای بعدی است، نه تضمین حذف امن دیسک هنگام قطع برق.

3. VMess parser: conversion واسطه‌ای حذف شد. WS، TCP HTTP، gRPC، HTTP/H2، HTTPUpgrade، KCP، QUIC و XHTTP/SplitHTTP به تنظیمات مربوط تبدیل می‌شوند؛ TLS/SNI/ALPN، fingerprint، gRPC authority/serviceName/multiMode و گزینه‌های صریح Xray حفظ می‌شوند. transport ناشناخته صریحاً رد می‌شود. QR متن اصلی را برای مورد بدون ویرایش نگه می‌دارد؛ بازاشتراک‌گذاری ویرایش‌شده نیز transport object کامل را منتقل می‌کند.

4. URI decoder: + خام به فاصله تبدیل نمی‌شود؛ فقط percent decoding انجام می‌شود.

5. Glass focus: در حالت عادی border دائمی اضافه نشده؛ edge accent با شدت focus/hover رسم می‌شود و داخل TextBox ثابت می‌ماند.

6. Glow: کش 300 Bitmap حذف شد. مسیر و طول محیط برای geometry نگه داشته می‌شود؛ فقط Reflection فعلی با feather رسم می‌شود. stripهای دارای اتصال تخت جای segmentهای RoundCap را گرفتند. سرعت 10 ثانیه، تأخیر کارت‌ها، reduced motion و عدم بازنقاشی متن آمار حفظ شدند. این renderer در WinForms/GDI+ است و ادعای GPU-only ندارد.

7. Power button: بدون انتخاب سرور، پیام انگلیسی/فارسی قابل‌دیدن نمایش داده می‌شود و فوکوس به فهرست سرورها می‌رود.

8. SOCKS statistics: از StatsService خود Xray روی loopback استفاده می‌شود؛ مجموع داده و نرخ از counter واقعی inbound پروکسی می‌آیند. TUN همچنان از شمارندهٔ رابط TUN استفاده می‌کند. Disconnect شمارنده‌ها و مقادیر نمایشی نشست را پاک می‌کند؛ پاسخ دیررس به نشست بعدی اعمال نمی‌شود.

9. Updater cleanup: stage خراب/لغوشده، ZIP دانلودی و backup موقت پس از موفقیت یا rollback پاک می‌شوند. نتیجهٔ کوچک نصب تا اجرای بعدی باقی می‌ماند؛ ابتدا خوانده و سپس پوشه‌اش حذف می‌شود. در شکست نصب پیام نمایش داده می‌شود. backup بازیابیِ ناموفق برای بازیابی حفظ می‌شود. Worker با مسیر مطلق PowerShell و فرمان inline اجرا می‌شود؛ محتوای فایل script قابل‌تعویض اجرا نمی‌شود و فایل stage بین hash و copy قفل است.

## تست‌ها
- Build و Publish Customer: صفر خطا و هشدار.
- هستهٔ exe و DLL دستکاری‌شده پیش از اجرای Process رد شدند؛ فایل بررسی‌شده هنگام اجرا قابل نوشتن نبود.
- Cleanup session قدیمی و برخورد با فایل قفل‌شده پاس شدند.
- آزمون تصویری focus edge بدون تغییر داخل input پاس شد.
- ALPN/TLS/SNI/TCP HTTP/gRPC و + خام، همراه با بازاشتراک‌گذاری VMess ویرایش‌شده پاس شدند.
- نصب‌کنندهٔ واقعی PowerShell در پوشهٔ آزمایشی اجرا شد: موفقیت، rollback بعد از قفل فایل مقصد، حفظ data، حذف staging/backup و خواندن نتیجهٔ موفق/ناموفق پاس شدند.
- هستهٔ واقعی Xray با Stats API اجرا شد؛ دانلود 32KB روی HTTP محلی از SOCKS عبور کرد و دو counter افزایش یافتند. TUN روشن نبود.
- UI، زبان‌ها، تعویض تم، navigation، layout، scrolling، عدم repaint متن آمار و reduced motion پاس شدند.
- Profile Customer: صفر گروه ذخیره‌شده و encrypted roundtrip موفق.
- DPI در 125/150/200٪ هنوز تست واقعی نشده؛ Corner hotspot به‌عنوان باگ قطعی گزارش نمی‌شود.
- تست شبکهٔ مشتری با credential واقعی اجرا نشده؛ تست انتقال، محلی و قابل تکرار بوده است.

قرارداد Stats API از منبع رسمی Xray:
https://github.com/XTLS/Xray-core/blob/main/app/stats/command/command.proto
