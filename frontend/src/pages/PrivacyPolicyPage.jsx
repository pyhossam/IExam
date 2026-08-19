
import React from "react";

export default function PrivacyPolicyPage() {
  return (
    <main dir="rtl" className="min-h-screen bg-gray-50 p-6">
      <div className="max-w-5xl mx-auto bg-white shadow rounded-2xl p-8">
        
        <h1 className="text-4xl font-bold mb-6 text-center">
          سياسة الخصوصية
        </h1>

        <p className="mb-6 text-gray-600 text-center">
          آخر تحديث: 08 May 2026
        </p>

        <section className="space-y-6 text-gray-700 leading-8">

          <div>
            <h2 className="text-2xl font-bold mb-2">1. مقدمة</h2>
            <p>
              يلتزم تطبيق IExam بحماية خصوصية المستخدمين والطلاب
              والمدارس، ويتم استخدام البيانات فقط لأغراض تعليمية
              وإدارية داخل النظام.
            </p>
          </div>

          <div>
            <h2 className="text-2xl font-bold mb-2">
              2. البيانات التي نجمعها
            </h2>

            <ul className="list-disc pr-6 space-y-2">
              <li>بيانات الطلاب</li>
              <li>نتائج الاختبارات</li>
              <li>بيانات أولياء الأمور</li>
              <li>بيانات تسجيل الدخول</li>
              <li>بيانات الاستخدام والأمان</li>
            </ul>
          </div>

          <div>
            <h2 className="text-2xl font-bold mb-2">
              3. استخدام البيانات
            </h2>

            <p>
              تستخدم البيانات لإدارة الاختبارات الإلكترونية،
              متابعة أداء الطلاب، وتحسين تجربة المستخدم.
            </p>
          </div>

          <div>
            <h2 className="text-2xl font-bold mb-2">
              4. حماية البيانات
            </h2>

            <p>
              يتم استخدام وسائل حماية تقنية مناسبة لمنع الوصول
              غير المصرح به إلى البيانات.
            </p>
          </div>

          <div>
            <h2 className="text-2xl font-bold mb-2">
              5. التواصل
            </h2>

            <p>
              للاستفسارات:
            </p>

            <div className="bg-gray-100 rounded-xl p-4 mt-3">
              <p>Email: hossam.kfuac@gmail.com</p>
              <p>Mobile: +966 54 224 5788</p>
              <p>Website: https://hossamsarhan.com</p>
            </div>
          </div>

        </section>
      </div>
    </main>
  );
}
