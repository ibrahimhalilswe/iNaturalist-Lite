# 🌱 iNaturalist-Lite: Yapay Zeka Destekli Biyoçeşitlilik Keşif Platformu

> **VTYS Dersi Dönem Projesi**  
> *"Gör, Çek, Keşfet, Koru."*

![Project Banner](https://img.freepik.com/premium-photo/abstract-glowing-green-neural-network-futuristic-technology-concept-artificial-intelligence-3d-rendering_36682-78823.jpg)

---

## 📖 Proje Hakkında
**iNaturalist-Lite**, kullanıcıların çevrelerindeki bitkileri fotoğraflayıp yapay zeka ile tanımlamasını ve bu gözlemleri harita üzerinde görselleştirmesini sağlayan bir biyoçeşitlilik toplama platformudur.

Proje; **PostGIS**, **.NET Core API**, **Pl@ntNet AI**, **Leaflet Harita**, **OpenStreetMap** gibi tamamen **ücretsiz ve açık kaynak** teknolojiler üzerine inşa edilmiştir.

Amaç; kullanıcıların çevresindeki bitki türlerini kolayca tanımlamasını, kaydetmesini ve büyüyen topluluk verisiyle yerel biyoçeşitliliği haritalandırmaktır.

---

## 🚀 Özellikler

### 🌸 Yapay Zeka ile Bitki Tanıma  
Fotoğraf Pl@ntNet API’sine gönderilir, tanımlama sonucu:  
- **Bitki Adı (`name`)**  
- **Güven skoru ve açıklama (`description`)**  
otomatik oluşturulur.

### 📍 Coğrafi Konum Desteği
Tüm gözlemler şu verilerle birlikte kaydedilir:
- **Enlem (`lat`)**
- **Boylam (`lng`)**
- **PostGIS Geometry Point (`location`)**

### 🗺️ Canlı Harita  
Leaflet tabanlı harita:
- Marker üzerinde bitki adı  
- Küçük önizleme görseli  
- Açıklama  
- Kullanıcı rozeti  
- Zaman bilgisi  
ile birlikte görüntülenir.

### 🏆 Oyunlaştırma Sistemi
Her kullanıcının:
- Kullanıcı adı (`username`)  
- Rozeti (`userbadge`)  
sisteme kaydedilir ve haritada gösterilir.

---

## 🛠️ Teknoloji Mimarisi

| Katman | Teknoloji | Açıklama |
|-------|-----------|----------|
| Backend | .NET 8 Minimal API | API uçları, dosya yükleme, veri işleme |
| Veritabanı | PostgreSQL + PostGIS | Mekansal veri desteği |
| Yapay Zeka | **Pl@ntNet API** | Bitki tanıma |
| Frontend | HTML, CSS, JS | Upload, AI, harita |
| Harita | LeafletJS + OpenStreetMap | Gözlem görselleştirme |

---

## ⚙️ Sistem İş Akışı
1. 📤 Kullanıcı fotoğraf yükler  
2. 🤖 Backend fotoğrafı **Pl@ntNet AI** servisine gönderir  
3. 🔍 Yapay zeka bitkiyi tanımlar  
4. 🗺️ Veriler PostgreSQL + PostGIS içine kaydedilir  
5. 📍 Harita tüm verileri dinamik olarak çeker  
6. 🖼️ Marker üzerinde fotoğraf + bitki adı görüntülenir  

---

## 🗄️ Veritabanı Yapısı
Backend şu sütunları kullanmaktadır:

| Sütun | Tür | Açıklama |
|------|-----|----------|
| id | SERIAL | Birincil anahtar |
| name | VARCHAR | Bitki adı |
| description | TEXT | AI açıklaması + güven skoru |
| photourl | TEXT | Yüklenen fotoğrafın yolu |
| createdat | TIMESTAMP | Kayıt zamanı |
| location | Geometry(Point, 4326) | Enlem/Boylam saklama |
| username | TEXT | Gözlemci kullanıcı |
| userbadge | TEXT | Kullanıcı rozeti |
| lat | DOUBLE | Enlem (opsiyonel) |
| lng | DOUBLE | Boylam (opsiyonel) |

### 🎯 Güncel SQL Tablo Yapısı

```sql
-- PostGIS Eklentisi Aktifleştirme
CREATE EXTENSION IF NOT EXISTS postgis;

-- Plants Tablosu
CREATE TABLE Plants (
    id SERIAL PRIMARY KEY,
    name VARCHAR(100),                -- Bitki adı (AI sonucu)
    description TEXT,                  -- AI güven skoru ve detaylar
    photourl TEXT,                     -- Fotoğraf yolu
    createdat TIMESTAMP DEFAULT NOW(), -- Kayıt zamanı
    location GEOMETRY(Point, 4326),    -- PostGIS koordinat verisi
    username TEXT DEFAULT 'Misafir',   -- Kullanıcı adı
    userbadge TEXT DEFAULT '🌱',        -- Kullanıcı rozeti
    lat DOUBLE PRECISION,              -- Enlem (isteğe bağlı)
    lng DOUBLE PRECISION               -- Boylam (isteğe bağlı)
);
