using Reboot.Models;

namespace Reboot.Services;

public class InMemoryDataStore : IMarketplaceDataStore
{
    public List<User> Users { get; } = new();
    public List<Listing> Listings { get; } = new();
    public List<Offer> Offers { get; } = new();
    public List<Order> Orders { get; } = new();
    public List<Dispute> Disputes { get; } = new();
    public List<Review> Reviews { get; } = new();

    public object SyncRoot { get; } = new();

    public InMemoryDataStore()
    {
        SeedData();
    }

    private void SeedData()
    {
        var defaultHash = PasswordHasher.HashPassword("Password123!");

        var admin = new User
        {
            Id = "usr-admin",
            FirstName = "Marco",
            LastName = "Tan",
            DisplayName = "Marco T.",
            Email = "admin@reboot.ph",
            PasswordHash = defaultHash,
            Location = "Cebu City",
            JoinedDate = new DateTime(2024, 1, 15, 8, 0, 0, DateTimeKind.Utc),
            IsVerified = true,
            IsAdmin = true
        };

        var dave = new User
        {
            Id = "usr-dave",
            FirstName = "Dave",
            LastName = "Alcantara",
            DisplayName = "Dave A.",
            Email = "dave@reboot.ph",
            PasswordHash = defaultHash,
            Location = "Mandaue City",
            JoinedDate = new DateTime(2024, 3, 10, 10, 30, 0, DateTimeKind.Utc),
            IsVerified = true,
            IsAdmin = false
        };

        var kirsten = new User
        {
            Id = "usr-kirsten",
            FirstName = "Kirsten",
            LastName = "Lim",
            DisplayName = "Kirsten L.",
            Email = "kirsten@reboot.ph",
            PasswordHash = defaultHash,
            Location = "Cebu City",
            JoinedDate = new DateTime(2024, 8, 22, 14, 15, 0, DateTimeKind.Utc),
            IsVerified = true,
            IsAdmin = false
        };

        var ramon = new User
        {
            Id = "usr-ramon",
            FirstName = "Ramon",
            LastName = "Yap",
            DisplayName = "Ramon Y.",
            Email = "ramon@reboot.ph",
            PasswordHash = defaultHash,
            Location = "Lapu-Lapu City",
            JoinedDate = new DateTime(2025, 2, 5, 9, 0, 0, DateTimeKind.Utc),
            IsVerified = true,
            IsAdmin = false
        };

        var paolo = new User
        {
            Id = "usr-paolo",
            FirstName = "Paolo",
            LastName = "Garcia",
            DisplayName = "Paolo G.",
            Email = "paolo@reboot.ph",
            PasswordHash = defaultHash,
            Location = "Talisay City",
            JoinedDate = new DateTime(2025, 6, 12, 16, 45, 0, DateTimeKind.Utc),
            IsVerified = true,
            IsAdmin = false
        };

        var angelo = new User
        {
            Id = "usr-angelo",
            FirstName = "Angelo",
            LastName = "Gomez",
            DisplayName = "Angelo G.",
            Email = "angelo@reboot.ph",
            PasswordHash = defaultHash,
            Location = "Cebu City",
            JoinedDate = new DateTime(2026, 10, 1, 11, 0, 0, DateTimeKind.Utc),
            IsVerified = false, // Unverified new user with no history
            IsAdmin = false
        };

        Users.AddRange(new[] { admin, dave, kirsten, ramon, paolo, angelo });

        // ---------------------------------------------------------------------
        // 2. Listings (17 Believable Items with Condition Reports and Checklist)
        // ---------------------------------------------------------------------
        var now = DateTime.UtcNow;

        var l1 = new Listing
        {
            Id = Guid.Parse("11111111-1111-1111-1111-111111111101"),
            SellerId = dave.Id,
            Title = "PlayStation 5 Disc Edition (CFI-1218A) with DualSense",
            Brand = "Sony",
            Category = ListingCategory.Console,
            Price = 24500,
            MinimumOffer = 23000,
            Description = "CFI-1218A model bought brand new from Datablitz. Used gently in air-conditioned room. Complete with original box, HDMI 2.1 cable, stand, and power cord. Factory reset and ready for new owner.",
            Grade = ItemGrade.A,
            ConditionReport = new ConditionReport
            {
                KnownDefects = "None. High-gloss center strip has microscratches typical of PS5 piano finish.",
                Checks = new List<ConditionCheck>
                {
                    new() { Name = "Boots properly", Passed = true, Notes = "Fast boot under 15s to home screen" },
                    new() { Name = "All ports functional (HDMI, USB, Power)", Passed = true, Notes = "HDMI 2.1 120Hz confirmed, USB ports charge controllers" },
                    new() { Name = "Disc drive / cartridge slot reads", Passed = true, Notes = "Tested with Spider-Man 2 disc, instant read" },
                    new() { Name = "No overheating after 30 min", Passed = true, Notes = "Ran Astro's Playroom for 45 min, fan quiet" },
                    new() { Name = "Controller pairs wirelessly", Passed = true, Notes = "Instant Bluetooth sync" }
                }
            },
            Photos = new List<string> { "ps5_front.jpg", "ps5_ports.jpg", "ps5_box.jpg" },
            SerialNumber = "CFI1218A-8492041",
            WarrantyOffered = 14,
            MeetupOrShipping = DeliveryOption.Both,
            Status = ListingStatus.Active,
            CreatedDate = now.AddDays(-6)
        };

        var l2 = new Listing
        {
            Id = Guid.Parse("11111111-1111-1111-1111-111111111102"),
            SellerId = dave.Id,
            Title = "DualSense Wireless Controller Midnight Black",
            Brand = "Sony",
            Category = ListingCategory.Controller,
            Price = 2800,
            MinimumOffer = 2500,
            Description = "Backup controller used only when friends visited. Analog sticks are tight and calibrated with zero stick drift on gamepad tester.",
            Grade = ItemGrade.A,
            ConditionReport = new ConditionReport
            {
                KnownDefects = "None. Clean matte texture intact.",
                Checks = new List<ConditionCheck>
                {
                    new() { Name = "No stick drift", Passed = true, Notes = "Deadzone circularity 6.2%, centered perfectly" },
                    new() { Name = "All buttons and triggers register", Passed = true, Notes = "Adaptive triggers click crisply" },
                    new() { Name = "Battery holds charge", Passed = true, Notes = "Estimated 7-8 hours play time" },
                    new() { Name = "Wireless pairing works", Passed = true, Notes = "Tested on PS5 and PC" },
                    new() { Name = "Vibration / haptics work", Passed = true, Notes = "Haptic rumble fully operational" }
                }
            },
            Photos = new List<string> { "dualsense_black_front.jpg", "dualsense_back.jpg" },
            SerialNumber = "AK8CFIZCT1W",
            WarrantyOffered = 7,
            MeetupOrShipping = DeliveryOption.Both,
            Status = ListingStatus.Active,
            CreatedDate = now.AddDays(-4)
        };

        var l3 = new Listing
        {
            Id = Guid.Parse("11111111-1111-1111-1111-111111111103"),
            SellerId = kirsten.Id,
            Title = "Steam Deck OLED 512GB Handheld Console",
            Brand = "Valve",
            Category = ListingCategory.Handheld,
            Price = 28500,
            MinimumOffer = 27000,
            Description = "Valve Steam Deck OLED 512GB model. Beautiful vibrant OLED panel. Upgraded with matte anti-glare screen protector since day one. Includes original carrying case and 45W charger.",
            Grade = ItemGrade.A,
            ConditionReport = new ConditionReport
            {
                KnownDefects = "None. Clean housing, no scuffs on corners.",
                Checks = new List<ConditionCheck>
                {
                    new() { Name = "Screen has no dead pixels or deep scratches", Passed = true, Notes = "Spotless 90Hz OLED panel" },
                    new() { Name = "Buttons and thumbsticks responsive", Passed = true, Notes = "Trackpads and haptics working" },
                    new() { Name = "Battery health >80%", Passed = true, Notes = "Battery health reports 97% in SteamOS desktop" },
                    new() { Name = "Speakers and headphone jack work", Passed = true, Notes = "Clear stereo audio" },
                    new() { Name = "Charging port functional", Passed = true, Notes = "45W USB-PD fast charge verified" }
                }
            },
            Photos = new List<string> { "steamdeck_oled_front.jpg", "steamdeck_case.jpg" },
            SerialNumber = "FXBC019482",
            WarrantyOffered = 30,
            MeetupOrShipping = DeliveryOption.Both,
            Status = ListingStatus.Active,
            CreatedDate = now.AddDays(-5)
        };

        var l4 = new Listing
        {
            Id = Guid.Parse("11111111-1111-1111-1111-111111111104"),
            SellerId = ramon.Id,
            Title = "Nintendo Switch OLED White Joy-Con Bundle",
            Brand = "Nintendo",
            Category = ListingCategory.Console,
            Price = 11800,
            MinimumOffer = 11000,
            Description = "Complete package with dock, white Joy-Cons, grip, straps, and AC adapter. Minor scuff on dock corner, screen is pristine with tempered glass.",
            Grade = ItemGrade.B,
            ConditionReport = new ConditionReport
            {
                KnownDefects = "Small hairline rub mark on lower back plastic near kickstand. Dock has slight scuff.",
                Checks = new List<ConditionCheck>
                {
                    new() { Name = "Boots properly", Passed = true, Notes = "Boots quickly into Nintendo Horizon OS" },
                    new() { Name = "All ports functional (HDMI, USB, Power)", Passed = true, Notes = "Dock output tested on 4K TV" },
                    new() { Name = "Disc drive / cartridge slot reads", Passed = true, Notes = "Tested with Zelda BOTW card" },
                    new() { Name = "No overheating after 30 min", Passed = true, Notes = "Quiet fan in docked mode" },
                    new() { Name = "Controller pairs wirelessly", Passed = true, Notes = "Both Joy-Cons rail and wireless tested" }
                }
            },
            Photos = new List<string> { "switch_oled_white.jpg", "switch_dock.jpg" },
            SerialNumber = "XKW100829104",
            WarrantyOffered = 7,
            MeetupOrShipping = DeliveryOption.Both,
            Status = ListingStatus.Active,
            CreatedDate = now.AddDays(-8)
        };

        var l5 = new Listing
        {
            Id = Guid.Parse("11111111-1111-1111-1111-111111111105"),
            SellerId = kirsten.Id,
            Title = "ASUS ROG Zephyrus G14 Gaming Laptop (Ryzen 9 5900HS / RTX 3060 / 16GB)",
            Brand = "ASUS",
            Category = ListingCategory.Laptop,
            Price = 44000,
            MinimumOffer = 41000,
            Description = "Compact 14-inch powerhouse laptop. Moonlight White without AniMe Matrix. 144Hz WQHD IPS display, 1TB NVMe SSD. Re-pasted with Honeywell PTM7950 phase change pad for stellar thermals.",
            Grade = ItemGrade.B,
            ConditionReport = new ConditionReport
            {
                KnownDefects = "Slight keyboard spacebar shine from typing. Bottom rubber feet have slight wear.",
                Checks = new List<ConditionCheck>
                {
                    new() { Name = "Boots to OS smoothly", Passed = true, Notes = "Windows 11 Home license activated" },
                    new() { Name = "Display clear with no lines or dead pixels", Passed = true, Notes = "WQHD panel has accurate colors, no backlight bleed" },
                    new() { Name = "Keyboard and touchpad responsive", Passed = true, Notes = "All keys and glass touchpad smooth" },
                    new() { Name = "Battery holds charge on unplug", Passed = true, Notes = "Battery report shows 83% wear level" },
                    new() { Name = "Wi-Fi and Bluetooth connect", Passed = true, Notes = "Intel AX200 Wi-Fi 6 tested" }
                }
            },
            Photos = new List<string> { "g14_front.jpg", "g14_keyboard.jpg" },
            SerialNumber = "MCN0CV0394850",
            WarrantyOffered = 14,
            MeetupOrShipping = DeliveryOption.Meetup,
            Status = ListingStatus.Active,
            CreatedDate = now.AddDays(-3)
        };

        var l6 = new Listing
        {
            Id = Guid.Parse("11111111-1111-1111-1111-111111111106"),
            SellerId = dave.Id,
            Title = "NVIDIA GeForce RTX 3070 Founders Edition 8GB GDDR6",
            Brand = "NVIDIA",
            Category = ListingCategory.PCPart,
            Price = 16500,
            MinimumOffer = 15500,
            Description = "Iconic dual axial flow-through cooler design. Clean card never used for crypto mining, only casual gaming. Box and 12-pin PCIe adapter included.",
            Grade = ItemGrade.A,
            ConditionReport = new ConditionReport
            {
                KnownDefects = "None. Clean heatsink fins, dust-free.",
                Checks = new List<ConditionCheck>
                {
                    new() { Name = "POSTs without error", Passed = true, Notes = "Instant UEFI display on DP 1.4" },
                    new() { Name = "Stress test passed without artifacts or crash", Passed = true, Notes = "3DMark Time Spy 20 loops: 99.1% stability" },
                    new() { Name = "Fan(s) spin quietly without bearing rattle", Passed = true, Notes = "0dB idle mode works, smooth ramp up" },
                    new() { Name = "All display outputs / connectors work", Passed = true, Notes = "3x DisplayPort and 1x HDMI 2.1 tested" },
                    new() { Name = "No thermal throttling under normal load", Passed = true, Notes = "Max temp 71°C at 220W TDP" }
                }
            },
            Photos = new List<string> { "rtx3070_fe.jpg", "rtx3070_box.jpg" },
            SerialNumber = "142392004928",
            WarrantyOffered = 14,
            MeetupOrShipping = DeliveryOption.Both,
            Status = ListingStatus.Active,
            CreatedDate = now.AddDays(-7)
        };

        var l7 = new Listing
        {
            Id = Guid.Parse("11111111-1111-1111-1111-111111111107"),
            SellerId = ramon.Id,
            Title = "Xbox Series X 1TB Black Console",
            Brand = "Microsoft",
            Category = ListingCategory.Console,
            Price = 22000,
            MinimumOffer = 20500,
            Description = "12-teraflop Microsoft flagship. Ultra-fast Quick Resume. Includes black wireless controller and ultra high speed HDMI cable. Quiet and powerful.",
            Grade = ItemGrade.A,
            ConditionReport = new ConditionReport
            {
                KnownDefects = "None. Matte black surface clean with no gouges.",
                Checks = new List<ConditionCheck>
                {
                    new() { Name = "Boots properly", Passed = true, Notes = "Clean boot into Xbox Dashboard" },
                    new() { Name = "All ports functional (HDMI, USB, Power)", Passed = true, Notes = "Storage expansion slot and USB tested" },
                    new() { Name = "Disc drive / cartridge slot reads", Passed = true, Notes = "Tested 4K UHD Blu-ray movie disc" },
                    new() { Name = "No overheating after 30 min", Passed = true, Notes = "Whisper-quiet exhaust fan" },
                    new() { Name = "Controller pairs wirelessly", Passed = true, Notes = "Xbox Wireless protocol verified" }
                }
            },
            Photos = new List<string> { "xbox_series_x.jpg" },
            SerialNumber = "094820194823",
            WarrantyOffered = 14,
            MeetupOrShipping = DeliveryOption.Both,
            Status = ListingStatus.Active,
            CreatedDate = now.AddDays(-10)
        };

        var l8 = new Listing
        {
            Id = Guid.Parse("11111111-1111-1111-1111-111111111108"),
            SellerId = dave.Id,
            Title = "Xbox Elite Wireless Controller Series 2 Black",
            Brand = "Microsoft",
            Category = ListingCategory.Controller,
            Price = 6200,
            MinimumOffer = 5500,
            Description = "Premium controller with adjustable tension thumbsticks, wrap-around rubberized grip, and shorter hair trigger locks. Carrying case, paddles, and charging dock included.",
            Grade = ItemGrade.B,
            ConditionReport = new ConditionReport
            {
                KnownDefects = "Slight rubber loosening on the bottom seam of right hand grip (cosmetic only, does not peel).",
                Checks = new List<ConditionCheck>
                {
                    new() { Name = "No stick drift", Passed = true, Notes = "Tension dials firm, centers cleanly" },
                    new() { Name = "All buttons and triggers register", Passed = true, Notes = "Hair trigger stops and paddles click reliably" },
                    new() { Name = "Battery holds charge", Passed = true, Notes = "Internal battery holds 35+ hours" },
                    new() { Name = "Wireless pairing works", Passed = true, Notes = "Bluetooth and Xbox Wireless" },
                    new() { Name = "Vibration / haptics work", Passed = true, Notes = "Impulse triggers functional" }
                }
            },
            Photos = new List<string> { "elite2_front.jpg", "elite2_case.jpg" },
            SerialNumber = "049281039481",
            WarrantyOffered = 7,
            MeetupOrShipping = DeliveryOption.Both,
            Status = ListingStatus.Active,
            CreatedDate = now.AddDays(-12)
        };

        var l9 = new Listing
        {
            Id = Guid.Parse("11111111-1111-1111-1111-111111111109"),
            SellerId = kirsten.Id,
            Title = "SteelSeries Arctis Nova 7 Wireless Gaming Headset",
            Brand = "SteelSeries",
            Category = ListingCategory.Accessory,
            Price = 5200,
            MinimumOffer = 4800,
            Description = "Simultaneous 2.4GHz gaming audio and Bluetooth phone audio. ClearCast Gen 2 retractable microphone. AirWeave ear cushions washed and sanitized.",
            Grade = ItemGrade.A,
            ConditionReport = new ConditionReport
            {
                KnownDefects = "None. Headband elastic is firm and non-stretched.",
                Checks = new List<ConditionCheck>
                {
                    new() { Name = "Connects and operates reliably", Passed = true, Notes = "USB-C dongle instant connection" },
                    new() { Name = "Cable / plug has no fraying or damage", Passed = true, Notes = "Charging cable and 3.5mm cable intact" },
                    new() { Name = "All dials / switches work", Passed = true, Notes = "ChatMix dial and volume wheel smooth" },
                    new() { Name = "Cosmetic surface is clean", Passed = true, Notes = "Earcups and ski goggle band spotless" }
                }
            },
            Photos = new List<string> { "arctis_nova7.jpg" },
            SerialNumber = "61553-90821",
            WarrantyOffered = 7,
            MeetupOrShipping = DeliveryOption.Both,
            Status = ListingStatus.Active,
            CreatedDate = now.AddDays(-2)
        };

        var l10 = new Listing
        {
            Id = Guid.Parse("11111111-1111-1111-1111-111111111110"),
            SellerId = ramon.Id,
            Title = "Keychron Q1 Pro Wireless QMK/VIA Custom Mechanical Keyboard",
            Brand = "Keychron",
            Category = ListingCategory.Accessory,
            Price = 7500,
            MinimumOffer = 7000,
            Description = "Full CNC aluminum body, double-gasket mount design. Keychron K Pro Banana tactile switches. Bluetooth 5.1 and Type-C wired. Heavy and ultra-premium.",
            Grade = ItemGrade.A,
            ConditionReport = new ConditionReport
            {
                KnownDefects = "None. Anodized black finish has zero scratches.",
                Checks = new List<ConditionCheck>
                {
                    new() { Name = "Connects and operates reliably", Passed = true, Notes = "Both BT switch and USB-C wired working" },
                    new() { Name = "Cable / plug has no fraying or damage", Passed = true, Notes = "Stock braided cable included" },
                    new() { Name = "All dials / switches work", Passed = true, Notes = "Rotary knob controls volume/mute" },
                    new() { Name = "Cosmetic surface is clean", Passed = true, Notes = "Keycaps free of oil/shine" }
                }
            },
            Photos = new List<string> { "keychron_q1.jpg" },
            SerialNumber = "Q1P-B1-94820",
            WarrantyOffered = 14,
            MeetupOrShipping = DeliveryOption.Both,
            Status = ListingStatus.Active,
            CreatedDate = now.AddDays(-15)
        };

        var l11 = new Listing
        {
            Id = Guid.Parse("11111111-1111-1111-1111-111111111111"),
            SellerId = dave.Id,
            Title = "ASUS ROG Ally Z1 Extreme Handheld 512GB (Upgraded 1TB SSD)",
            Brand = "ASUS",
            Category = ListingCategory.Handheld,
            Price = 24000,
            MinimumOffer = 22500,
            Description = "ROG Ally Z1 Extreme version upgraded to 1TB Micron 2230 NVMe SSD. 120Hz 1080p screen with FreeSync Premium. Comes with Dbrand Killswitch case with kickstand.",
            Grade = ItemGrade.B,
            ConditionReport = new ConditionReport
            {
                KnownDefects = "MicroSD card reader occasionally fails to mount high-capacity cards (known Ally hardware issue). Built-in 1TB SSD works 100% perfectly.",
                Checks = new List<ConditionCheck>
                {
                    new() { Name = "Screen has no dead pixels or deep scratches", Passed = true, Notes = "Pristine IPS 120Hz display" },
                    new() { Name = "Buttons and thumbsticks responsive", Passed = true, Notes = "Zero stick drift" },
                    new() { Name = "Battery health >80%", Passed = true, Notes = "91% battery health" },
                    new() { Name = "Speakers and headphone jack work", Passed = true, Notes = "Smart AMP speakers very loud and clear" },
                    new() { Name = "Charging port functional", Passed = true, Notes = "65W ROG charger supported" }
                }
            },
            Photos = new List<string> { "rog_ally.jpg" },
            SerialNumber = "R9NRCX029381",
            WarrantyOffered = 7,
            MeetupOrShipping = DeliveryOption.Both,
            Status = ListingStatus.Active,
            CreatedDate = now.AddDays(-9)
        };

        var l12 = new Listing
        {
            Id = Guid.Parse("11111111-1111-1111-1111-111111111112"),
            SellerId = kirsten.Id,
            Title = "AMD Ryzen 7 5800X3D Desktop Processor (8C/16T, 96MB 3D V-Cache)",
            Brand = "AMD",
            Category = ListingCategory.PCPart,
            Price = 13500,
            MinimumOffer = 12500,
            Description = "The undisputed king of AM4 gaming CPUs. Undervolted with -30 all-core Curve Optimizer since purchase. No bent pins, pins are laser-straight. Original box included.",
            Grade = ItemGrade.A,
            ConditionReport = new ConditionReport
            {
                KnownDefects = "None. Heatspreader has faint thermal paste imprint, easily cleaned.",
                Checks = new List<ConditionCheck>
                {
                    new() { Name = "POSTs without error", Passed = true, Notes = "Recognized in B550/X570 boards" },
                    new() { Name = "Stress test passed without artifacts or crash", Passed = true, Notes = "Prime95 and Cinebench R23 30m test pass" },
                    new() { Name = "Fan(s) spin quietly without bearing rattle", Passed = true, Notes = "N/A - processor only" },
                    new() { Name = "All display outputs / connectors work", Passed = true, Notes = "All 1331 AM4 pins intact" },
                    new() { Name = "No thermal throttling under normal load", Passed = true, Notes = "Max 74°C with dual tower air cooler" }
                }
            },
            Photos = new List<string> { "ryzen_5800x3d.jpg" },
            SerialNumber = "9JM8192048102",
            WarrantyOffered = 14,
            MeetupOrShipping = DeliveryOption.Both,
            Status = ListingStatus.Active,
            CreatedDate = now.AddDays(-11)
        };

        var l13 = new Listing
        {
            Id = Guid.Parse("11111111-1111-1111-1111-111111111113"),
            SellerId = ramon.Id,
            Title = "Lenovo Legion 5 15ACH6 (Ryzen 7 5800H / RTX 3050Ti / 16GB RAM)",
            Brand = "Lenovo",
            Category = ListingCategory.Laptop,
            Price = 29500,
            MinimumOffer = 27000,
            Description = "Reliable eSports gaming laptop. TrueStrike keyboard with white backlight, 165Hz 100% sRGB display. 300W Lenovo power brick included. Minor scuffs on lid edge.",
            Grade = ItemGrade.C,
            ConditionReport = new ConditionReport
            {
                KnownDefects = "Noticeable scratch on top lid near Lenovo metal badge. Battery health is at 72% capacity (gives about 2.5 hours non-gaming). One rubber foot missing.",
                Checks = new List<ConditionCheck>
                {
                    new() { Name = "Boots to OS smoothly", Passed = true, Notes = "Fast SSD boot" },
                    new() { Name = "Display clear with no lines or dead pixels", Passed = true, Notes = "165Hz panel in great condition" },
                    new() { Name = "Keyboard and touchpad responsive", Passed = true, Notes = "All keys register" },
                    new() { Name = "Battery holds charge on unplug", Passed = false, Notes = "Degraded to 72% health, holds 2.5 hours light use" },
                    new() { Name = "Wi-Fi and Bluetooth connect", Passed = true, Notes = "Realtek Wi-Fi works well" }
                }
            },
            Photos = new List<string> { "legion5_lid.jpg", "legion5_open.jpg" },
            SerialNumber = "PF38K901",
            WarrantyOffered = null, // None
            MeetupOrShipping = DeliveryOption.Meetup,
            Status = ListingStatus.Active,
            CreatedDate = now.AddDays(-14)
        };

        var l14 = new Listing
        {
            Id = Guid.Parse("11111111-1111-1111-1111-111111111114"),
            SellerId = dave.Id,
            Title = "Nintendo Switch Lite Turquoise (For Parts / Repair)",
            Brand = "Nintendo",
            Category = ListingCategory.Handheld,
            Price = 2800,
            MinimumOffer = null,
            Description = "Switch Lite Turquoise that boots to orange error screen after water droplet entered top vent. Screen and buttons are intact. Selling strictly as-is for parts, battery, shell, or technician repair.",
            Grade = ItemGrade.ForParts,
            ConditionReport = new ConditionReport
            {
                KnownDefects = "Orange screen error (error code 2101-0001 / M92T36 power IC failure suspected). Does not boot into OS.",
                Checks = new List<ConditionCheck>
                {
                    new() { Name = "Screen has no dead pixels or deep scratches", Passed = true, Notes = "LCD panel illuminates without cracks" },
                    new() { Name = "Buttons and thumbsticks responsive", Passed = true, Notes = "Left thumbstick feels slightly stiff" },
                    new() { Name = "Battery health >80%", Passed = false, Notes = "Unknown, system does not charge past standby" },
                    new() { Name = "Speakers and headphone jack work", Passed = false, Notes = "Untested due to boot failure" },
                    new() { Name = "Charging port functional", Passed = false, Notes = "Draws only 0.4A at 5V, power IC issue" }
                }
            },
            Photos = new List<string> { "switch_lite_turquoise.jpg" },
            SerialNumber = "XJE100928371",
            WarrantyOffered = null,
            MeetupOrShipping = DeliveryOption.Both,
            Status = ListingStatus.Active,
            CreatedDate = now.AddDays(-3)
        };

        var l15 = new Listing
        {
            Id = Guid.Parse("11111111-1111-1111-1111-111111111115"),
            SellerId = ramon.Id,
            Title = "8BitDo Ultimate 2.4G Wireless Controller with Charging Dock",
            Brand = "8BitDo",
            Category = ListingCategory.Controller,
            Price = 1750,
            MinimumOffer = 1500,
            Description = "Hall Effect magnetic sensing joystick version (no stick drift forever!). Custom profile switch button. Complete with charging dock and USB 2.4GHz receiver.",
            Grade = ItemGrade.A,
            ConditionReport = new ConditionReport
            {
                KnownDefects = "None. Dock and controller are pristine.",
                Checks = new List<ConditionCheck>
                {
                    new() { Name = "No stick drift", Passed = true, Notes = "Hall Effect sticks test 0.0000% error rate" },
                    new() { Name = "All buttons and triggers register", Passed = true, Notes = "Crisp membrane face buttons" },
                    new() { Name = "Battery holds charge", Passed = true, Notes = "Holds 15 hours per charge" },
                    new() { Name = "Wireless pairing works", Passed = true, Notes = "2.4G dongle and Bluetooth functional" },
                    new() { Name = "Vibration / haptics work", Passed = true, Notes = "Asymmetric rumble motors functional" }
                }
            },
            Photos = new List<string> { "8bitdo_ultimate.jpg" },
            SerialNumber = "8BD-ULT-82910",
            WarrantyOffered = 7,
            MeetupOrShipping = DeliveryOption.Both,
            Status = ListingStatus.Active,
            CreatedDate = now.AddDays(-4)
        };

        var l16 = new Listing
        {
            Id = Guid.Parse("11111111-1111-1111-1111-111111111116"),
            SellerId = dave.Id,
            Title = "Elgato HD60 X External Game Capture Card (4K60 HDR10 Passthrough)",
            Brand = "Elgato",
            Category = ListingCategory.Accessory,
            Price = 7900,
            MinimumOffer = 7200,
            Description = "Capture PS5 or Xbox gameplay at 1080p60 HDR or 1440p60 with zero lag passthrough to monitor. Includes high quality braided USB-C to USB-A cable and HDMI 2.0 cable.",
            Grade = ItemGrade.A,
            ConditionReport = new ConditionReport
            {
                KnownDefects = "None. Rubber pad underneath is clean.",
                Checks = new List<ConditionCheck>
                {
                    new() { Name = "Connects and operates reliably", Passed = true, Notes = "Detected in OBS Studio and Elgato 4K Capture" },
                    new() { Name = "Cable / plug has no fraying or damage", Passed = true, Notes = "OEM cables in new condition" },
                    new() { Name = "All dials / switches work", Passed = true, Notes = "Status LED bar indicates active capture correctly" },
                    new() { Name = "Cosmetic surface is clean", Passed = true, Notes = "Clean matte black exterior" }
                }
            },
            Photos = new List<string> { "elgato_hd60x.jpg" },
            SerialNumber = "ELG-10GBE9901-8392",
            WarrantyOffered = 14,
            MeetupOrShipping = DeliveryOption.Both,
            Status = ListingStatus.Active,
            CreatedDate = now.AddDays(-1)
        };

        // Reserved listing for seed order
        var l17 = new Listing
        {
            Id = Guid.Parse("11111111-1111-1111-1111-111111111117"),
            SellerId = dave.Id,
            Title = "Corsair RM850x 850W 80 PLUS Gold Fully Modular Power Supply",
            Brand = "Corsair",
            Category = ListingCategory.PCPart,
            Price = 4500,
            MinimumOffer = 4000,
            Description = "Tier-A PSU with Japanese 105°C capacitors and magnetic levitation fan. All modular cables present in cable pouch.",
            Grade = ItemGrade.A,
            ConditionReport = new ConditionReport
            {
                KnownDefects = "None. Dust filter kept interior spotless.",
                Checks = new List<ConditionCheck>
                {
                    new() { Name = "POSTs without error", Passed = true, Notes = "Voltages measured: 12.04V, 5.01V, 3.32V with digital tester" },
                    new() { Name = "Stress test passed without artifacts or crash", Passed = true, Notes = "Tested with 700W draw for 1 hour" },
                    new() { Name = "Fan(s) spin quietly without bearing rattle", Passed = true, Notes = "Zero RPM mode at low loads" },
                    new() { Name = "All display outputs / connectors work", Passed = true, Notes = "All 24-pin, 8-pin EPS, PCIe, and SATA tested" },
                    new() { Name = "No thermal throttling under normal load", Passed = true, Notes = "Quiet mag-lev fan operation" }
                }
            },
            Photos = new List<string> { "corsair_rm850x.jpg" },
            SerialNumber = "CP-9020200-NA-1892",
            WarrantyOffered = 30,
            MeetupOrShipping = DeliveryOption.Both,
            Status = ListingStatus.Reserved,
            CreatedDate = now.AddDays(-2)
        };

        // Listing for order in Paid (held)
        var l18 = new Listing
        {
            Id = Guid.Parse("11111111-1111-1111-1111-111111111118"),
            SellerId = ramon.Id,
            Title = "Sony DualSense Charging Station (Official OEM)",
            Brand = "Sony",
            Category = ListingCategory.Accessory,
            Price = 1200,
            MinimumOffer = 1000,
            Description = "Official click-in dock charges two DualSense controllers simultaneously. Complete with AC power adapter.",
            Grade = ItemGrade.A,
            ConditionReport = new ConditionReport
            {
                KnownDefects = "None.",
                Checks = new List<ConditionCheck>
                {
                    new() { Name = "Connects and operates reliably", Passed = true, Notes = "Both charging docks connect smoothly" },
                    new() { Name = "Cable / plug has no fraying or damage", Passed = true, Notes = "Original power brick and cable intact" },
                    new() { Name = "All dials / switches work", Passed = true, Notes = "Click-in spring mechanism crisp" },
                    new() { Name = "Cosmetic surface is clean", Passed = true, Notes = "Clean white/black shell" }
                }
            },
            Photos = new List<string> { "dualsense_dock.jpg" },
            SerialNumber = "CFI-ZDS1",
            WarrantyOffered = 7,
            MeetupOrShipping = DeliveryOption.Shipping,
            Status = ListingStatus.Reserved,
            CreatedDate = now.AddDays(-3)
        };

        // Listing for order in HandedOver (inspection window active)
        var l19 = new Listing
        {
            Id = Guid.Parse("11111111-1111-1111-1111-111111111119"),
            SellerId = kirsten.Id,
            Title = "Razer DeathAdder V3 Pro Wireless Gaming Mouse White",
            Brand = "Razer",
            Category = ListingCategory.Accessory,
            Price = 4200,
            MinimumOffer = 3900,
            Description = "63g ultra-lightweight ergonomic mouse. Focus Pro 30K Optical Sensor and Gen-3 Optical Switches (zero double click). Includes dongle, extender, and USB-C Speedflex cable.",
            Grade = ItemGrade.A,
            ConditionReport = new ConditionReport
            {
                KnownDefects = "None. PTFE skates have smooth glide with no grooves.",
                Checks = new List<ConditionCheck>
                {
                    new() { Name = "Connects and operates reliably", Passed = true, Notes = "Instant 2.4GHz wireless tracking" },
                    new() { Name = "Cable / plug has no fraying or damage", Passed = true, Notes = "USB cable pristine" },
                    new() { Name = "All dials / switches work", Passed = true, Notes = "Scroll wheel and side buttons crisp" },
                    new() { Name = "Cosmetic surface is clean", Passed = true, Notes = "White shell has zero yellowing or grime" }
                }
            },
            Photos = new List<string> { "deathadder_v3_white.jpg" },
            SerialNumber = "RZ01-04630200",
            WarrantyOffered = 14,
            MeetupOrShipping = DeliveryOption.Both,
            Status = ListingStatus.Reserved,
            CreatedDate = now.AddDays(-4)
        };

        // Listing for order in Disputed
        var l20 = new Listing
        {
            Id = Guid.Parse("11111111-1111-1111-1111-111111111120"),
            SellerId = ramon.Id,
            Title = "Logitech G Pro X Superlight Wireless Gaming Mouse Black",
            Brand = "Logitech",
            Category = ListingCategory.Accessory,
            Price = 3600,
            MinimumOffer = 3200,
            Description = "Superlight wireless mouse with HERO 25K sensor. Listed as Grade A.",
            Grade = ItemGrade.B,
            ConditionReport = new ConditionReport
            {
                KnownDefects = "None stated by seller.",
                Checks = new List<ConditionCheck>
                {
                    new() { Name = "Connects and operates reliably", Passed = true, Notes = "Connects via Lightspeed dongle" },
                    new() { Name = "Cable / plug has no fraying or damage", Passed = true, Notes = "Charging cable included" },
                    new() { Name = "All dials / switches work", Passed = true, Notes = "Scroll wheel tactile" },
                    new() { Name = "Cosmetic surface is clean", Passed = true, Notes = "Clean coating" }
                }
            },
            Photos = new List<string> { "gprox_superlight.jpg" },
            SerialNumber = "2049LZ82941",
            WarrantyOffered = 7,
            MeetupOrShipping = DeliveryOption.Shipping,
            Status = ListingStatus.Reserved,
            CreatedDate = now.AddDays(-5)
        };

        Listings.AddRange(new[] { l1, l2, l3, l4, l5, l6, l7, l8, l9, l10, l11, l12, l13, l14, l15, l16, l17, l18, l19, l20 });

        // ---------------------------------------------------------------------
        // 3. Orders in Various Escrow States
        // ---------------------------------------------------------------------

        // Order 1: Reserved (Buyer: Paolo, Seller: Dave) - 24h countdown active
        var ordReserved = new Order
        {
            Id = Guid.Parse("22222222-2222-2222-2222-222222222201"),
            ListingId = l17.Id,
            BuyerId = paolo.Id,
            SellerId = dave.Id,
            Price = l17.Price,
            DeliveryMethod = DeliveryMethod.Meetup,
            Status = OrderStatus.Reserved,
            CreatedDate = now.AddHours(-6),
            ReservedUntil = now.AddHours(18) // 18 hours left in 24h window
        };

        // Order 2: Paid (held) (Buyer: Kirsten, Seller: Ramon) - Payment secured, awaiting seller handover
        var ordPaid = new Order
        {
            Id = Guid.Parse("22222222-2222-2222-2222-222222222202"),
            ListingId = l18.Id,
            BuyerId = kirsten.Id,
            SellerId = ramon.Id,
            Price = l18.Price,
            DeliveryMethod = DeliveryMethod.Shipping,
            Status = OrderStatus.Paid,
            CreatedDate = now.AddDays(-1).AddHours(-4),
            ReservedUntil = now.AddDays(-1).AddHours(20),
            PaidDate = now.AddHours(-14)
        };

        // Order 3: HandedOverOrShipped (Buyer: Paolo, Seller: Kirsten) - 48h inspection window running
        var ordInspection = new Order
        {
            Id = Guid.Parse("22222222-2222-2222-2222-222222222203"),
            ListingId = l19.Id,
            BuyerId = paolo.Id,
            SellerId = kirsten.Id,
            Price = l19.Price,
            DeliveryMethod = DeliveryMethod.Meetup,
            Status = OrderStatus.HandedOverOrShipped,
            CreatedDate = now.AddDays(-2),
            ReservedUntil = now.AddDays(-1),
            PaidDate = now.AddDays(-1).AddHours(-18),
            HandedOverDate = now.AddHours(-16),
            InspectionDeadline = now.AddHours(32) // 32 hours remaining in 48h window
        };

        // Order 4: Disputed (Buyer: Dave, Seller: Ramon) - Open dispute under admin review
        var ordDisputed = new Order
        {
            Id = Guid.Parse("22222222-2222-2222-2222-222222222204"),
            ListingId = l20.Id,
            BuyerId = dave.Id,
            SellerId = ramon.Id,
            Price = l20.Price,
            DeliveryMethod = DeliveryMethod.Shipping,
            Status = OrderStatus.Disputed,
            CreatedDate = now.AddDays(-3),
            ReservedUntil = now.AddDays(-2),
            PaidDate = now.AddDays(-2).AddHours(-12),
            HandedOverDate = now.AddDays(-2),
            InspectionDeadline = now.AddHours(-8),
            DisputedDate = now.AddHours(-12)
        };

        // Order 5: Released (Completed sale Dave -> Paolo, reviewed)
        var ordReleased1 = new Order
        {
            Id = Guid.Parse("22222222-2222-2222-2222-222222222205"),
            ListingId = Guid.NewGuid(),
            BuyerId = paolo.Id,
            SellerId = dave.Id,
            Price = 8500,
            DeliveryMethod = DeliveryMethod.Meetup,
            Status = OrderStatus.Released,
            CreatedDate = now.AddDays(-30),
            ReservedUntil = now.AddDays(-29),
            PaidDate = now.AddDays(-29).AddHours(4),
            HandedOverDate = now.AddDays(-29).AddHours(8),
            InspectionDeadline = now.AddDays(-27).AddHours(8),
            ConfirmedDate = now.AddDays(-28),
            ReleasedDate = now.AddDays(-28)
        };

        // Order 6: Released (Completed sale Dave -> Kirsten, reviewed)
        var ordReleased2 = new Order
        {
            Id = Guid.Parse("22222222-2222-2222-2222-222222222206"),
            ListingId = Guid.NewGuid(),
            BuyerId = kirsten.Id,
            SellerId = dave.Id,
            Price = 14200,
            DeliveryMethod = DeliveryMethod.Meetup,
            Status = OrderStatus.Released,
            CreatedDate = now.AddDays(-45),
            ReservedUntil = now.AddDays(-44),
            PaidDate = now.AddDays(-44).AddHours(2),
            HandedOverDate = now.AddDays(-44).AddHours(6),
            InspectionDeadline = now.AddDays(-42).AddHours(6),
            ConfirmedDate = now.AddDays(-43),
            ReleasedDate = now.AddDays(-43)
        };

        // Order 7: Released (Completed sale Kirsten -> Paolo, reviewed)
        var ordReleased3 = new Order
        {
            Id = Guid.Parse("22222222-2222-2222-2222-222222222207"),
            BuyerId = paolo.Id,
            SellerId = kirsten.Id,
            ListingId = Guid.NewGuid(),
            Price = 3500,
            DeliveryMethod = DeliveryMethod.Shipping,
            Status = OrderStatus.Released,
            CreatedDate = now.AddDays(-60),
            ReservedUntil = now.AddDays(-59),
            PaidDate = now.AddDays(-59).AddHours(1),
            HandedOverDate = now.AddDays(-58),
            InspectionDeadline = now.AddDays(-56),
            ConfirmedDate = now.AddDays(-57),
            ReleasedDate = now.AddDays(-57)
        };

        // Additional completed sales for Dave to establish veteran reputation
        for (int i = 1; i <= 12; i++)
        {
            var pastOrder = new Order
            {
                Id = Guid.NewGuid(),
                ListingId = Guid.NewGuid(),
                BuyerId = (i % 2 == 0) ? paolo.Id : kirsten.Id,
                SellerId = dave.Id,
                Price = 3000 + (i * 800),
                DeliveryMethod = (i % 2 == 0) ? DeliveryMethod.Meetup : DeliveryMethod.Shipping,
                Status = OrderStatus.Released,
                CreatedDate = now.AddDays(-70 - (i * 15)),
                ReservedUntil = now.AddDays(-69 - (i * 15)),
                PaidDate = now.AddDays(-69 - (i * 15)),
                HandedOverDate = now.AddDays(-68 - (i * 15)),
                InspectionDeadline = now.AddDays(-66 - (i * 15)),
                ConfirmedDate = now.AddDays(-67 - (i * 15)),
                ReleasedDate = now.AddDays(-67 - (i * 15))
            };
            Orders.Add(pastOrder);

            // Add corresponding review
            Reviews.Add(new Review
            {
                Id = Guid.NewGuid(),
                OrderId = pastOrder.Id,
                ReviewerId = pastOrder.BuyerId,
                SubjectId = dave.Id,
                Rating = (i == 4) ? 4 : 5,
                Comment = i switch
                {
                    1 => "Condition checklist was 100% accurate. Smooth meetup at Ayala Center Cebu.",
                    2 => "Super clean gear. Dave tested everything in front of me before releasing.",
                    3 => "Fast response, honest seller. Escrow gave complete peace of mind.",
                    4 => "Good item, minor box dent not noted but device itself works perfectly.",
                    _ => "Excellent transaction. As described, verified seller you can trust."
                },
                CreatedDate = pastOrder.ReleasedDate!.Value.AddHours(2)
            });
        }

        Orders.AddRange(new[] { ordReserved, ordPaid, ordInspection, ordDisputed, ordReleased1, ordReleased2, ordReleased3 });

        // ---------------------------------------------------------------------
        // 4. Disputes
        // ---------------------------------------------------------------------
        var disp1 = new Dispute
        {
            Id = Guid.Parse("33333333-3333-3333-3333-333333333301"),
            OrderId = ordDisputed.Id,
            OpenedById = dave.Id,
            Reason = "Left click switch double clicks intermittently. Not disclosed on checklist.",
            EvidenceNotes = "Buyer tested in Gamepad / Mouse debouncing test suite: 18 out of 100 clicks recorded as unintentional double clicks. Seller listed condition report with 'All dials / switches work: Passed'. Buyer requested partial refund of ₱800 for replacement Japanese Omron D2F switches or full return.",
            Status = DisputeStatus.Open,
            AdminNote = null,
            CreatedDate = now.AddHours(-12)
        };
        Disputes.Add(disp1);

        // ---------------------------------------------------------------------
        // 5. Offers (Pending and Historical)
        // ---------------------------------------------------------------------
        var off1 = new Offer
        {
            Id = Guid.Parse("44444444-4444-4444-4444-444444444401"),
            ListingId = l1.Id,
            BuyerId = paolo.Id,
            Amount = 23500,
            Status = OfferStatus.Pending,
            CreatedDate = now.AddHours(-3)
        };

        var off2 = new Offer
        {
            Id = Guid.Parse("44444444-4444-4444-4444-444444444402"),
            ListingId = l3.Id,
            BuyerId = dave.Id,
            Amount = 27000,
            Status = OfferStatus.Pending,
            CreatedDate = now.AddHours(-8)
        };

        var off3 = new Offer
        {
            Id = Guid.Parse("44444444-4444-4444-4444-444444444403"),
            ListingId = l6.Id,
            BuyerId = paolo.Id,
            Amount = 15800,
            Status = OfferStatus.Pending,
            CreatedDate = now.AddHours(-16)
        };

        Offers.AddRange(new[] { off1, off2, off3 });

        // ---------------------------------------------------------------------
        // 6. Reviews for existing released orders
        // ---------------------------------------------------------------------
        Reviews.Add(new Review
        {
            Id = Guid.NewGuid(),
            OrderId = ordReleased1.Id,
            ReviewerId = paolo.Id,
            SubjectId = dave.Id,
            Rating = 5,
            Comment = "Prompt meetup at Cebu IT Park. Item was in like-new condition, condition report checklist was completely accurate.",
            CreatedDate = now.AddDays(-28)
        });

        Reviews.Add(new Review
        {
            Id = Guid.NewGuid(),
            OrderId = ordReleased2.Id,
            ReviewerId = kirsten.Id,
            SubjectId = dave.Id,
            Rating = 5,
            Comment = "Smooth escrow transaction. Funds held safely until I inspected the hardware. Highly recommended seller.",
            CreatedDate = now.AddDays(-43)
        });

        Reviews.Add(new Review
        {
            Id = Guid.NewGuid(),
            OrderId = ordReleased3.Id,
            ReviewerId = paolo.Id,
            SubjectId = kirsten.Id,
            Rating = 5,
            Comment = "Shipped through LBC carefully packed with bubble wrap. Verified seller, trustworthy.",
            CreatedDate = now.AddDays(-57)
        });
    }
}
