# 🌱 iNaturalist-Lite: Yapay Zeka Destekli Biyoçeşitlilik Kaşifi

> **VTYS Dersi Dönem Projesi**
> *"Gör, Çek, Keşfet, Koru."*

![Project Banner](https://img.freepik.com/premium-photo/abstract-glowing-green-neural-network-futuristic-technology-concept-artificial-intelligence-3d-rendering_36682-78823.jpg)

## 📖 Proje Hakkında
**iNaturalist-Lite**, kullanıcıların çevrelerindeki bitkileri fotoğraflayarak yapay zeka yardımıyla tanımlamasını sağlayan, **PostGIS** tabanlı ve tamamen **açık kaynaklı** bir yerel biyoçeşitlilik platformudur.

Çoğu insan çevresindeki türleri tanımamakta ve bu değerli biyoçeşitlilik verileri kaybolmaktadır. Bu proje, vatandaş bilimi (citizen science) mantığıyla bu verileri kayıt altına almayı ve haritalandırmayı amaçlar.

## 🚀 Özellikler
* **🌸 Hassas Tanımlama:** **Pl@ntNet API** entegrasyonu sayesinde bitki türlerinin yüksek doğrulukla teşhis edilmesi.
* **📍 Konum İşleme:** PostgreSQL & PostGIS ile çekilen fotoğrafın konumunun (Enlem/Boylam) veritabanına işlenmesi.
* **🗺️ Görselleştirme:** OpenStreetMap ve Leaflet ile canlı biyoçeşitlilik haritası.
* **🏆 Oyunlaştırma:** Kullanıcılar için rozet sistemi (`userbadge`) ve keşif takibi.

## 🛠️ Teknoloji Mimarisi (Tech Stack)
Proje tamamen ücretsiz ve açık kaynaklı teknolojiler üzerine kurulmuştur:

| Alan | Teknoloji | Açıklama |
|---|---|---|
| **Data** | PostgreSQL & PostGIS | İlişkisel ve Coğrafi Veri Tabanı |
| **Backend** | .NET Core API | Sistem yönetimi ve API servisleri |
| **AI / Identification** | **Pl@ntNet API** | Özelleşmiş Bitki Tanıma Servisi |
| **Map** | OpenStreetMap & Leaflet | Konum görselleştirme |

## ⚙️ Nasıl Çalışır? (İş Akışı)
Sistem asenkron bir yapıda çalışarak kullanıcı deneyimini optimize eder:

1.  **Upload:** Kullanıcı bitki fotoğrafını sisteme yükler.
2.  **API Request:** Backend, fotoğrafı **Pl@ntNet API** servisine iletir.
3.  **Identification:** API, görseli analiz eder ve bitki adını (`name`) belirler.
4.  **Database Log:** Bitki adı, konumu ve kullanıcı bilgileri veritabanına kaydedilir.

## 🗄️ Veritabanı Tasarımı
Proje, mekansal sorgular için **PostGIS** eklentisini kullanır. Gerçek tablo yapısı şöyledir:

```sql
-- PostGIS Eklentisi Aktifleştirme
CREATE EXTENSION IF NOT EXISTS postgis;

-- Tablo Yapısı
CREATE TABLE Plants (
    id SERIAL PRIMARY KEY,
    name VARCHAR(100),           -- Bitki Adı (AI'dan gelen)
    description TEXT,            -- AI Güven Skoru ve Detaylar
    photourl TEXT,               -- Sunucudaki dosya yolu
    createdat TIMESTAMP DEFAULT NOW(),   -- Keşif Zamanı
    location GEOMETRY(Point, 4326),      -- PostGIS Konum Verisi (Enlem/Boylam)
    username TEXT DEFAULT 'Misafir',     -- Keşfi Yapan Kullanıcı
    userbadge TEXT DEFAULT '🌱'          -- Kullanıcı Rozeti
);
