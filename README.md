# 🌱 iNaturalist-Lite: Yapay Zeka Destekli Biyoçeşitlilik Kaşifi

> **VTYS Dersi Dönem Projesi**
> *"Gör, Çek, Keşfet, Koru."*

![Project Banner](https://img.freepik.com/premium-photo/abstract-glowing-green-neural-network-futuristic-technology-concept-artificial-intelligence-3d-rendering_36682-78823.jpg)

## 📖 Proje Hakkında
**iNaturalist-Lite**, kullanıcıların çevrelerindeki bitki ve hayvanları fotoğraflayarak yapay zeka yardımıyla tanımlamasını sağlayan, **PostGIS** tabanlı ve tamamen **açık kaynaklı** bir yerel biyoçeşitlilik platformudur.

Çoğu insan çevresindeki türleri tanımamakta ve bu değerli biyoçeşitlilik verileri kaybolmaktadır. Bu proje, vatandaş bilimi (citizen science) mantığıyla bu verileri kayıt altına almayı ve haritalandırmayı amaçlar.

## 🚀 Özellikler
* **🤖 AI Analizi:** Hugging Face Vision Transformers kullanılarak %95'e varan doğrulukla tür tahmini.
* **📍 Konum İşleme:** PostgreSQL & PostGIS ile çekilen fotoğrafın konumunun haritalandırılması.
* **🗺️ Görselleştirme:** OpenStreetMap ve Leaflet ile canlı biyoçeşitlilik haritası.
* **🏆 Oyunlaştırma:** Yeni tür keşiflerinde kullanıcılara rozet sistemi.

## 🛠️ Teknoloji Mimarisi (Tech Stack)
Proje tamamen ücretsiz ve açık kaynaklı teknolojiler üzerine kurulmuştur:

| Alan | Teknoloji | Açıklama |
|---|---|---|
| **Data** | PostgreSQL & PostGIS | İlişkisel ve Coğrafi Veri Tabanı |
| **Backend** | .NET Core API | Sistem yönetimi ve API servisleri |
| **AI / ML** | Hugging Face API | Görüntü işleme ve sınıflandırma |
| **Map** | OpenStreetMap & Leaflet | Konum görselleştirme |

## ⚙️ Nasıl Çalışır? (İş Akışı)
Sistem asenkron bir yapıda çalışarak kullanıcı deneyimini optimize eder:

1.  **Upload:** Kullanıcı fotoğrafı sisteme yükler.
2.  **AI Request:** Backend, fotoğrafı Hugging Face Inference API'ye gönderir.
3.  **Identification:** AI, türü tanımlar (Örn: Papatya).
4.  **Database Log:** Tür bilgisi ve konum (Geometry/Point) veritabanına işlenir.

## 🗄️ Veritabanı Tasarımı
Proje, mekansal sorgular için PostGIS eklentisini kullanır. Temel tablo yapısı şöyledir:

```sql
CREATE TABLE Observations (
    id SERIAL PRIMARY KEY,
    user_id INT REFERENCES Users(id),
    image_url TEXT,
    ai_species VARCHAR(100),
    location GEOMETRY(POINT, 4326), -- PostGIS Konum Verisi
    created_at TIMESTAMP
);
