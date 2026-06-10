import { createClient } from '@supabase/supabase-js';

// Supabase projenize ait URL ve ANON KEY değerlerini ortam değişkenlerinden alıyoruz.
// React, Vite veya React Native ortamına göre process.env veya import.meta.env kullanılabilir.
const supabaseUrl = process.env.SUPABASE_URL || import.meta.env.VITE_SUPABASE_URL;
const supabaseAnonKey = process.env.SUPABASE_ANON_KEY || import.meta.env.VITE_SUPABASE_ANON_KEY;

if (!supabaseUrl || !supabaseAnonKey) {
  console.error("HATA: Supabase URL veya Anon Key bulunamadı!");
}

export const supabase = createClient(supabaseUrl, supabaseAnonKey);

/**
 * ==========================================
 * KIMLIK DOĞRULAMA (AUTH) SERVISLERI
 * ==========================================
 */

/**
 * Yeni bir kullanıcı kaydeder.
 * Ekstra kullanıcı bilgileri (username, badge) user_metadata olarak kaydedilir.
 * Supabase auth tarafında tetiklenen bir trigger ile bu veriler "users" tablosuna aktarılabilir.
 */
export async function signUp(email, password, username) {
  try {
    const { data, error } = await supabase.auth.signUp({
      email,
      password,
      options: {
        data: {
          username: username,
          badge: '🌱', // Varsayılan rozet
        },
      },
    });

    if (error) throw error;
    return { success: true, user: data.user, session: data.session };
  } catch (error) {
    console.error("Kayıt işlemi sırasında hata:", error.message);
    return { success: false, error: error.message };
  }
}

/**
 * Mevcut kullanıcının giriş yapmasını sağlar.
 */
export async function signIn(email, password) {
  try {
    const { data, error } = await supabase.auth.signInWithPassword({
      email,
      password,
    });

    if (error) throw error;
    return { success: true, user: data.user, session: data.session };
  } catch (error) {
    console.error("Giriş işlemi sırasında hata:", error.message);
    return { success: false, error: error.message };
  }
}

/**
 * E-posta adresine şifre sıfırlama bağlantısı gönderir.
 */
export async function forgotPassword(email) {
  try {
    const { data, error } = await supabase.auth.resetPasswordForEmail(email, {
      redirectTo: 'https://inaturalist-lite.vercel.app/reset-password',
    });

    if (error) throw error;
    return { success: true, data };
  } catch (error) {
    console.error("Şifre sıfırlama e-postası gönderilirken hata:", error.message);
    return { success: false, error: error.message };
  }
}

/**
 * Kullanıcının mevcut oturumunu kapatır.
 */
export async function logout() {
  try {
    const { error } = await supabase.auth.signOut();
    if (error) throw error;
    return { success: true };
  } catch (error) {
    console.error("Çıkış yapılırken hata oluştu:", error.message);
    return { success: false, error: error.message };
  }
}

/**
 * ==========================================
 * BİTKİ (PLANT/BIODIVERSITY) SERVISLERI
 * ==========================================
 */

/**
 * Sayfalanmış (paginated) şekilde bitki gözlemlerini getirir.
 */
export async function getPlants(page = 1, pageSize = 20) {
  try {
    const from = (page - 1) * pageSize;
    const to = from + pageSize - 1;

    const { data, error, count } = await supabase
      .from('plants')
      .select('*', { count: 'exact' })
      .order('createdat', { ascending: false })
      .range(from, to);

    if (error) throw error;

    return { success: true, plants: data, total: count, page, pageSize };
  } catch (error) {
    console.error("Bitkiler getirilirken hata:", error.message);
    return { success: false, error: error.message };
  }
}

/**
 * Yeni bir bitki gözlemi ekler. PostGIS mekaniklerini desteklemek için Location
 * verisi WKT (Well-Known Text) string formatı olan 'POINT(lng lat)' şeklinde gönderilir.
 */
export async function addPlant(plantData) {
  try {
    const { name, description, photoUrl, userName, userBadge, lat, lng } = plantData;
    
    // PostGIS Geometry tipine WKT (Well-Known Text) ile dönüştürme:
    // Not: PostGIS Point objelerinde sıralama Boylam(Lng) ve Enlem(Lat) şeklindedir!
    const locationWKT = (lat !== undefined && lng !== undefined) 
      ? `POINT(${lng} ${lat})` 
      : null;

    const { data, error } = await supabase
      .from('plants')
      .insert([
        {
          name,
          description,
          photourl: photoUrl,
          username: userName || 'Misafir',
          userbadge: userBadge || '🌱',
          location: locationWKT,
          createdat: new Date().toISOString()
        }
      ])
      .select();

    if (error) throw error;
    return { success: true, data };
  } catch (error) {
    console.error("Bitki eklenirken hata:", error.message);
    return { success: false, error: error.message };
  }
}

/**
 * İlgili ID'ye sahip bitki gözlemini siler (Yalnızca yetkili kullanıcı).
 */
export async function deletePlant(id) {
  try {
    const { data, error } = await supabase
      .from('plants')
      .delete()
      .eq('id', id);

    if (error) throw error;
    return { success: true, data };
  } catch (error) {
    console.error("Bitki silinirken hata:", error.message);
    return { success: false, error: error.message };
  }
}

/**
 * Genel istatistikleri çeker (Toplam Gözlem, Benzersiz Tür, Aktif Kullanıcı)
 */
export async function getStats() {
  try {
    // Toplam Gözlem Sayısı
    const { count: totalObservations, error: obsErr } = await supabase
      .from('plants')
      .select('*', { count: 'exact', head: true });
    
    if (obsErr) throw obsErr;

    // Supabase JS SDK'da DISTINCT doğrudan desteklenmez, bu nedenle 
    // veri setini çekip JS tarafında ayıklıyoruz veya özel bir RPC (Stored Procedure) yazıyoruz.
    // Şimdilik client-side unique count:
    const { data: nameData, error: nameErr } = await supabase.from('plants').select('name');
    if (nameErr) throw nameErr;
    const uniquePlants = new Set(nameData.map(p => p.name)).size;

    const { data: userData, error: userErr } = await supabase.from('plants').select('username');
    if (userErr) throw userErr;
    const activeUsers = new Set(userData.map(p => p.username)).size;

    return { success: true, stats: { totalObservations, uniquePlants, activeUsers } };
  } catch (error) {
    console.error("İstatistikler getirilirken hata:", error.message);
    return { success: false, error: error.message };
  }
}

/**
 * ==========================================
 * ETKİLEŞİM SERVISLERI (BEĞENİ & YORUM)
 * ==========================================
 */

/**
 * Bitkiyi beğenme (Toggle - Varsa siler, yoksa ekler).
 */
export async function toggleLikePlant(plantId, username) {
  try {
    // Önce beğeni var mı kontrol edelim
    const { data: existingLike, error: findError } = await supabase
      .from('plant_likes')
      .select('*')
      .eq('plant_id', plantId)
      .eq('username', username)
      .single();
    
    // single() bir şey bulamazsa PGRST116 (0 rows) hatası verir.
    if (existingLike) {
      // Varsa sil
      const { error: deleteError } = await supabase
        .from('plant_likes')
        .delete()
        .eq('id', existingLike.id);
      if (deleteError) throw deleteError;
      return { success: true, liked: false };
    } else {
      // Yoksa ekle
      const { error: insertError } = await supabase
        .from('plant_likes')
        .insert([{ plant_id: plantId, username }]);
      if (insertError) throw insertError;
      return { success: true, liked: true };
    }
  } catch (error) {
    // 0 row hatasını (PGRST116) yoksay ve ekleme yap
    if (error.code === 'PGRST116') {
      const { error: insertError } = await supabase
        .from('plant_likes')
        .insert([{ plant_id: plantId, username }]);
      if (insertError) {
        console.error("Beğeni eklerken hata:", insertError.message);
        return { success: false, error: insertError.message };
      }
      return { success: true, liked: true };
    }

    console.error("Beğeni işlemi sırasında hata:", error.message);
    return { success: false, error: error.message };
  }
}

/**
 * Bir bitkiye yeni yorum ekler.
 */
export async function addComment(plantId, username, text) {
  try {
    const { data, error } = await supabase
      .from('plant_comments')
      .insert([
        { 
          plant_id: plantId, 
          username, 
          text,
          created_at: new Date().toISOString()
        }
      ])
      .select();

    if (error) throw error;
    return { success: true, data };
  } catch (error) {
    console.error("Yorum eklenirken hata:", error.message);
    return { success: false, error: error.message };
  }
}

/**
 * Bir bitkinin yorumlarını getirir.
 */
export async function getComments(plantId) {
  try {
    const { data, error } = await supabase
      .from('plant_comments')
      .select('*')
      .eq('plant_id', plantId)
      .order('created_at', { ascending: true });

    if (error) throw error;
    return { success: true, comments: data };
  } catch (error) {
    console.error("Yorumlar getirilirken hata:", error.message);
    return { success: false, error: error.message };
  }
}

/**
 * ==========================================
 * KULLANICI PROFİLİ SERVISLERI
 * ==========================================
 */

/**
 * Kullanıcının public (herkese açık) profilini çeker.
 */
export async function getUserProfile(username) {
  try {
    // Kullanıcının sistemde olup olmadığını "users" tablosundan kontrol et (Varsa)
    const { data: user, error: userError } = await supabase
      .from('users')
      .select('*')
      .ilike('username', username)
      .single();
    
    if (userError) throw userError;

    const { count: totalObservations, error: obsErr } = await supabase
      .from('plants')
      .select('*', { count: 'exact', head: true })
      .ilike('username', username);

    const { data: plantsData } = await supabase.from('plants').select('name').ilike('username', username);
    const discoveredSpecies = plantsData ? new Set(plantsData.map(p => p.name)).size : 0;

    return { 
      success: true, 
      profile: {
        username: user.username,
        badge: user.badge,
        avatarUrl: user.avatar_url,
        createdAt: user.created_at,
        totalObservations,
        discoveredSpecies
      }
    };
  } catch (error) {
    console.error("Kullanıcı profili çekilirken hata:", error.message);
    return { success: false, error: error.message };
  }
}

/**
 * Bir kullanıcının kendi eklediği bitkileri getirir.
 */
export async function getUserPlants(username) {
  try {
    const { data, error } = await supabase
      .from('plants')
      .select('*')
      .ilike('username', username)
      .order('createdat', { ascending: false });

    if (error) throw error;
    return { success: true, plants: data };
  } catch (error) {
    console.error("Kullanıcının bitkileri getirilirken hata:", error.message);
    return { success: false, error: error.message };
  }
}
