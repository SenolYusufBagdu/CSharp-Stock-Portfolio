using System;
using System.Collections.Generic;
using System.IO;

class Hisse
{
    public string Sembol;
    public int Adet;
    public decimal Fiyat;
}

class Kullanici
{
    public string Ad;
    public List<Hisse> Hisseler = new List<Hisse>();
}

class Program
{
    static List<Kullanici> kullanicilar = new List<Kullanici>();
    static string dosya = "veriler.txt";

    static void Main()
    {
        DosyaOku();

        while (true)
        {
            Console.WriteLine("\n1- Kullanıcı Seç");
            Console.WriteLine("2- Yeni Kullanıcı");
            Console.WriteLine("3- Çıkış");
            Console.Write("Seçim: ");
            string secim = Console.ReadLine();

            if (secim == "1") KullaniciSec();
            else if (secim == "2") YeniKullanici();
            else if (secim == "3") break;
            else Console.WriteLine("Hatalı seçim");
        }
    }

  

    static void DosyaOku()
    {
        if (!File.Exists(dosya)) return;

        string[] satirlar = File.ReadAllLines(dosya);

        foreach (string s in satirlar)
        {
            string[] parca = s.Split('|');
            Kullanici k = new Kullanici();
            k.Ad = parca[0];

            if (parca.Length > 1)
            {
                string[] hisseler = parca[1].Split(';');
                foreach (string h in hisseler)
                {
                    string[] detay = h.Split(',');
                    k.Hisseler.Add(new Hisse
                    {
                        Sembol = detay[0],
                        Adet = int.Parse(detay[1]),
                        Fiyat = decimal.Parse(detay[2])
                    });
                }
            }
            kullanicilar.Add(k);
        }
    }

    static void DosyaYaz()
    {
        List<string> satirlar = new List<string>();

        foreach (Kullanici k in kullanicilar)
        {
            string satir = k.Ad;
            foreach (Hisse h in k.Hisseler)
                satir += $"|{h.Sembol},{h.Adet},{h.Fiyat}";
            satirlar.Add(satir);
        }
        File.WriteAllLines(dosya, satirlar);
    }

  

    static void YeniKullanici()
    {
        Console.Write("Kullanıcı adı: ");
        string ad = Console.ReadLine();
        kullanicilar.Add(new Kullanici { Ad = ad });
        DosyaYaz();
    }

    static void KullaniciSec()
    {
        Console.Write("Kullanıcı adı: ");
        string ad = Console.ReadLine();

        foreach (Kullanici k in kullanicilar)
        {
            if (k.Ad == ad)
            {
                KullaniciMenu(k);
                return;
            }
        }
        Console.WriteLine("Kullanıcı bulunamadı");
    }

    // ================= MENU =================

    static void KullaniciMenu(Kullanici k)
    {
        while (true)
        {
            Console.WriteLine($"\n--- {k.Ad} ---");
            Console.WriteLine("1- Hisseleri Listele");
            Console.WriteLine("2- Hisse Ekle");
            Console.WriteLine("3- Hisse Sil");
            Console.WriteLine("4- Hisse Transfer");
            Console.WriteLine("5- Net Değer");
            Console.WriteLine("6- Geri");
            Console.Write("Seçim: ");

            string secim = Console.ReadLine();

            if (secim == "1") Listele(k);
            else if (secim == "2") HisseEkle(k);
            else if (secim == "3") HisseSil(k);
            else if (secim == "4") Transfer(k);
            else if (secim == "5")
                Console.WriteLine("Net Değer: " + NetDeger(k));
            else if (secim == "6") break;
        }
    }

    // ================= İŞLEMLER =================

    static void Listele(Kullanici k)
    {
        foreach (Hisse h in k.Hisseler)
            Console.WriteLine($"{h.Sembol} - {h.Adet} adet - {h.Fiyat}");
    }

    static void HisseEkle(Kullanici k)
    {
        Console.Write("Sembol: ");
        string s = Console.ReadLine();
        Console.Write("Adet: ");
        int a = int.Parse(Console.ReadLine());
        Console.Write("Fiyat: ");
        decimal f = decimal.Parse(Console.ReadLine());

        k.Hisseler.Add(new Hisse { Sembol = s, Adet = a, Fiyat = f });
        DosyaYaz();
    }

    static void HisseSil(Kullanici k)
    {
        Console.Write("Silinecek sembol: ");
        string s = Console.ReadLine();

        for (int i = 0; i < k.Hisseler.Count; i++)
        {
            if (k.Hisseler[i].Sembol == s)
            {
                k.Hisseler.RemoveAt(i);
                DosyaYaz();
                return;
            }
        }
        Console.WriteLine("Hisse yok");
    }

    static void Transfer(Kullanici gonderen)
    {
        Console.Write("Alıcı: ");
        string aliciAd = Console.ReadLine();

        Kullanici alici = null;
        foreach (Kullanici k in kullanicilar)
            if (k.Ad == aliciAd) alici = k;

        if (alici == null) return;

        Console.Write("Sembol: ");
        string s = Console.ReadLine();

        foreach (Hisse h in gonderen.Hisseler)
        {
            if (h.Sembol == s)
            {
                alici.Hisseler.Add(h);
                gonderen.Hisseler.Remove(h);
                DosyaYaz();
                return;
            }
        }
    }

    static decimal NetDeger(Kullanici k)
    {
        decimal toplam = 0;
        foreach (Hisse h in k.Hisseler)
            toplam += h.Adet * h.Fiyat;
        return toplam;
    }
}
