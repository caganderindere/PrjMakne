namespace PrjProje.Infrastructure.Data;

/// <summary>
/// Full page texts of the manufacturing capabilities, keyed by slug.
/// Only h2, p and ul are used so the texts stay editable in the admin rich text editor.
/// </summary>
internal static class CapabilityContent
{
    public static readonly IReadOnlyDictionary<string, string> BySlug = new Dictionary<string, string>
    {
        ["cnc-isleme"] = """
            <p>Bir parçanın sahada beklenen performansı göstermesi, çoğu zaman son işlemedeki hassasiyete bağlıdır. CNC torna ve işleme merkezlerimizde, teknik resmi ya da numunesi paylaşılan parçaları hem seri hem de özel üretim olarak işliyoruz.</p>
            <h2>Neler yapıyoruz?</h2>
            <p>Döküm, dövme veya hammadde olarak gelen parçaları nihai ölçülerine getiriyor; yuvaları, delikleri ve bağlantı yüzeylerini teknik resimdeki değerlere göre işliyoruz. Döküm tesisimizden çıkan parçaların işlemesini de aynı çatı altında tamamladığımız için parça, tesisler arasında dolaşmadan bir sonraki aşamaya geçiyor.</p>
            <ul>
            <li>CNC tornalama ve frezeleme operasyonları</li>
            <li>Seri üretim ve özel üretim işleri</li>
            <li>Döküm ve kaynaklı parçaların son işlemesi</li>
            <li>Teknik resme veya numuneye göre üretim</li>
            </ul>
            <h2>Süreç nasıl işliyor?</h2>
            <p>Teknik resim önce mühendislik ekibimiz tarafından incelenir; bağlama yöntemi, operasyon sırası ve kesici takımlar parçaya göre belirlenir. İlk parça ölçülerek onaylandıktan sonra seri üretime geçilir, üretim boyunca kritik ölçüler kontrol edilir.</p>
            <h2>Size sağladığı fayda</h2>
            <p>İşleme, döküm, yüzey işlem ve kalite kontrolün aynı tesiste olması tedarikçi sayısını azaltır, teslim süresini kısaltır ve bir sorunun kaynağına hızla inilmesini sağlar.</p>
            """,
        ["cnc-machining"] = """
            <p>How a part performs in the field often comes down to the accuracy of its final machining. On our CNC lathes and machining centres, we produce parts from your technical drawing or sample, both in batches and as custom work.</p>
            <h2>What we do</h2>
            <p>We bring cast, forged or raw-stock parts to their final dimensions, machining seats, bores and mating surfaces to the values on the drawing. Because castings from our foundry are finished under the same roof, parts move to the next stage without travelling between facilities.</p>
            <ul>
            <li>CNC turning and milling operations</li>
            <li>Batch and custom production</li>
            <li>Finish machining of cast and welded parts</li>
            <li>Production from a technical drawing or sample</li>
            </ul>
            <h2>How the process works</h2>
            <p>Our engineering team first reviews the drawing and selects the clamping method, operation sequence and cutting tools for the part. Series production starts once the first part has been measured and approved, and critical dimensions are checked throughout the run.</p>
            <h2>What it means for you</h2>
            <p>Having machining, casting, surface finishing and quality control in one facility means fewer suppliers, shorter lead times and a faster route to the root of any problem.</p>
            """,

        ["hidrolik-pres"] = """
            <p>Kauçuğa son şeklini veren de, metale form kazandıran da doğru basınç ve doğru kalıptır. Farklı tonajlardaki hidrolik pres hatlarımızı hem kauçuk kalıplama hem de metal şekillendirme işlerinde kullanıyoruz.</p>
            <h2>Kauçuk kalıplama</h2>
            <p>Kendi tesisimizde hazırladığımız kauçuk hamurları, parçaya özel kalıplarda basınç ve sıcaklık altında şekillendirilir. Hamur reçetesinin de kalıbın da kontrolümüzde olması, parçanın uygulamadaki davranışını baştan planlamamızı sağlar.</p>
            <h2>Metal şekillendirme</h2>
            <p>Sac ve metal parçalar, kalıp arasında kontrollü basınçla istenen forma getirilir. Farklı tonajlarda pres bulunması, parçanın boyutuna ve malzemesine uygun hattı seçmemize olanak verir.</p>
            <ul>
            <li>Kauçuk parçaların kalıplanması</li>
            <li>Metal parçaların şekillendirilmesi</li>
            <li>Parçaya özel kalıplarla üretim</li>
            <li>Kalıp tasarımından seri üretime tek elden yönetim</li>
            </ul>
            <h2>Süreç nasıl işliyor?</h2>
            <p>Parçanın geometrisi ve kullanım koşulları değerlendirilir, kalıp tasarlanır ve deneme üretimiyle süreç değerleri belirlenir. Onaylanan numunenin ardından seri üretime geçilir ve parçalar kalite kontrolden geçirilerek sevk edilir.</p>
            """,
        ["hydraulic-pressing"] = """
            <p>Whether shaping rubber or forming metal, the result depends on the right pressure and the right tool. We use hydraulic press lines in a range of tonnages for both rubber moulding and metal forming.</p>
            <h2>Rubber moulding</h2>
            <p>Rubber compounds prepared in our own facility are shaped under pressure and heat in tools made for the part. Controlling both the compound formulation and the tool lets us plan how the part will behave in service from the very start.</p>
            <h2>Metal forming</h2>
            <p>Sheet and metal parts are brought to the required form under controlled pressure between dies. Having presses in different tonnages lets us choose the right line for the size and material of the part.</p>
            <ul>
            <li>Moulding of rubber parts</li>
            <li>Forming of metal parts</li>
            <li>Production with part-specific tooling</li>
            <li>One point of contact from tool design to series production</li>
            </ul>
            <h2>How the process works</h2>
            <p>We assess the part geometry and operating conditions, design the tool and set the process values through trial production. Series production follows sample approval, and parts pass through quality control before shipment.</p>
            """,

        ["kaucuk-hamur-karistirma"] = """
            <p>Bir kauçuk parçanın ömrünü, kalıptan çok önce hazırlanan hamur belirler. Bu nedenle kauçuk hamurlarımızı dışarıdan almak yerine, uygulamaya özel olarak kendi tesisimizde ve kontrollü reçetelerle hazırlıyoruz.</p>
            <h2>Neden kendi hamurumuz?</h2>
            <p>Aşınma, darbe, sıcaklık veya yağ teması gibi çalışma koşulları her parçada farklıdır. Hamuru kendimiz hazırladığımızda, reçeteyi parçanın maruz kalacağı koşullara göre şekillendirebilir ve partiler arasında tutarlılığı koruyabiliriz.</p>
            <ul>
            <li>Uygulamaya özel kauçuk hamur reçeteleri</li>
            <li>Kontrollü ve tekrarlanabilir karışım süreci</li>
            <li>Partiler arasında tutarlı malzeme kalitesi</li>
            <li>Kalıplama hatlarımızla doğrudan entegrasyon</li>
            </ul>
            <h2>Süreç nasıl işliyor?</h2>
            <p>Parçanın çalışacağı ortam ve beklenen performans değerlendirilerek reçete belirlenir. Hammaddeler reçeteye göre tartılır ve karıştırılır, hazırlanan hamur kontrol edildikten sonra hidrolik pres hatlarımızda kalıplama için kullanılır.</p>
            <h2>Size sağladığı fayda</h2>
            <p>Hamurdan bitmiş parçaya kadar tüm zincirin kendi tesisimizde olması, malzeme kaynaklı sorunların izlenmesini kolaylaştırır ve parçanın performansında iyileştirme gerektiğinde reçeteye doğrudan müdahale edebilmemizi sağlar.</p>
            """,
        ["rubber-compounding"] = """
            <p>The service life of a rubber part is decided by the compound, long before moulding. That is why we prepare our rubber compounds in-house, tailored to the application and made to tightly controlled formulations, rather than buying them in.</p>
            <h2>Why our own compound?</h2>
            <p>Operating conditions such as wear, impact, temperature or oil contact differ from part to part. Preparing the compound ourselves lets us shape the formulation around those conditions and keep it consistent from batch to batch.</p>
            <ul>
            <li>Application-specific rubber formulations</li>
            <li>A controlled, repeatable mixing process</li>
            <li>Consistent material quality across batches</li>
            <li>Direct integration with our moulding lines</li>
            </ul>
            <h2>How the process works</h2>
            <p>The formulation is set by assessing the part's working environment and expected performance. Raw materials are weighed and mixed to the formulation, and the compound is checked before it moves to moulding on our hydraulic press lines.</p>
            <h2>What it means for you</h2>
            <p>With the whole chain from compound to finished part in our own facility, material-related issues are easier to trace, and the formulation can be adjusted directly when a part's performance needs improving.</p>
            """,

        ["lazer-kesim"] = """
            <p>Sac ve plaka parçalarda üretimin ilk adımı çoğu zaman kesimdir ve bu adımın kalitesi sonraki tüm operasyonları etkiler. Lazer kesim hattımızda sac ve plaka malzemeleri yüksek hız ve hassasiyetle kesiyoruz.</p>
            <h2>Neden lazer kesim?</h2>
            <p>Lazer kesim, kesici takım veya parçaya özel kalıp gerektirmez; dijital çizimden doğrudan üretime geçilebilir. Bu sayede karmaşık geometriler de tek parçalık numuneler de seri işler kadar pratik biçimde kesilebilir.</p>
            <ul>
            <li>Sac ve plaka malzemelerin kesimi</li>
            <li>Karmaşık geometrilerin üretimi</li>
            <li>Numune ve seri üretim işleri</li>
            <li>Büküm ve kaynak için hazır kesim parçaları</li>
            </ul>
            <h2>Süreç nasıl işliyor?</h2>
            <p>Çizim dosyası kontrol edilir ve parçalar malzemeyi verimli kullanacak biçimde plakaya yerleştirilir. Kesilen parçalar, ihtiyaca göre büküm, işleme ve fırın boya hatlarımıza doğrudan aktarılır.</p>
            <h2>Size sağladığı fayda</h2>
            <p>Kesimden bükümüne, boyasından sevkiyatına kadar parçanın tek tesiste ilerlemesi, ara taşımaları ve bekleme sürelerini azaltır. Tasarım değişikliklerinde de yeni kalıp beklemeden çizim güncellenerek üretime devam edilebilir.</p>
            """,
        ["laser-cutting"] = """
            <p>Cutting is often the first step in making sheet and plate parts, and its quality affects every operation that follows. On our laser cutting line, we cut sheet and plate materials with high speed and precision.</p>
            <h2>Why laser cutting?</h2>
            <p>Laser cutting needs no part-specific die or cutting tool, so production can go straight from the digital drawing. Complex shapes and one-off samples can be cut as practically as batch work.</p>
            <ul>
            <li>Cutting of sheet and plate materials</li>
            <li>Production of complex geometries</li>
            <li>Sample and batch work</li>
            <li>Blanks ready for bending and welding</li>
            </ul>
            <h2>How the process works</h2>
            <p>The drawing file is checked and parts are nested on the plate to use material efficiently. Cut parts then move directly to our bending, machining and oven painting lines as required.</p>
            <h2>What it means for you</h2>
            <p>With a part moving from cutting through bending and painting to shipment in one facility, there is less transport and waiting between stages. When a design changes, production continues from the updated drawing without waiting for a new die.</p>
            """,

        ["bukum"] = """
            <p>Lazerde kesilen düz bir sacın işlevsel bir parçaya dönüştüğü aşama büküm operasyonudur. CNC abkant preslerimizde sac parçaları tekrarlanabilir ve hassas biçimde büküyoruz.</p>
            <h2>Neden CNC abkant?</h2>
            <p>CNC kontrollü büküm, açı ve ölçü değerlerini programa bağlar. Böylece aynı parça farklı üretim partilerinde de aynı sonuçla üretilebilir; bu da sonraki montaj ve kaynak işlemlerini kolaylaştırır.</p>
            <ul>
            <li>Sac parçaların CNC abkant preslerde bükülmesi</li>
            <li>Tekrarlanabilir açı ve ölçü</li>
            <li>Numune ve seri üretim işleri</li>
            <li>Lazer kesimle entegre üretim akışı</li>
            </ul>
            <h2>Süreç nasıl işliyor?</h2>
            <p>Parçanın açınımı ve büküm sırası teknik resme göre planlanır, uygun takım seçilir ve program hazırlanır. İlk parça ölçülerek kontrol edildikten sonra üretime devam edilir.</p>
            <h2>Size sağladığı fayda</h2>
            <p>Kesim, büküm ve yüzey işlemlerinin aynı tesiste yapılması, parçanın tek bir üretim akışında tamamlanmasını sağlar. Ölçüsü tutarlı parçalar montajda daha az uğraştırır ve zaman kazandırır.</p>
            """,
        ["bending"] = """
            <p>Bending is where a flat, laser-cut sheet becomes a functional part. On our CNC press brakes, we bend sheet metal parts precisely and repeatably.</p>
            <h2>Why CNC press brakes?</h2>
            <p>CNC-controlled bending ties angles and dimensions to a programme, so the same part comes out the same way across production batches. That in turn makes later assembly and welding easier.</p>
            <ul>
            <li>Bending of sheet parts on CNC press brakes</li>
            <li>Repeatable angles and dimensions</li>
            <li>Sample and batch work</li>
            <li>A production flow integrated with laser cutting</li>
            </ul>
            <h2>How the process works</h2>
            <p>The flat pattern and bend sequence are planned from the technical drawing, suitable tooling is selected and the programme is prepared. Production continues once the first part has been measured and checked.</p>
            <h2>What it means for you</h2>
            <p>Doing cutting, bending and surface finishing in one facility lets a part be completed in a single production flow. Dimensionally consistent parts take less effort at assembly and save time.</p>
            """,

        ["firin-boya"] = """
            <p>Boya, bir parçanın ilk göze çarpan yüzü olmasının yanında onu dış etkenlere karşı koruyan katmandır. 2 adet fırın boya tesisimizde parçalara dayanıklı ve homojen bir yüzey kazandırıyoruz.</p>
            <h2>Neden fırın boya?</h2>
            <p>Fırında kürlenen boya, parça yüzeyinde bütünlüklü ve dayanıklı bir tabaka oluşturur. Ağır hizmet koşullarında çalışan parçalar için bu dayanıklılık, görünüm kadar kullanım ömrü açısından da önemlidir.</p>
            <ul>
            <li>2 adet fırın boya tesisi</li>
            <li>Dayanıklı ve homojen yüzey</li>
            <li>Kesim, büküm ve işleme sonrası son kat uygulaması</li>
            <li>Üretim hattıyla entegre planlama</li>
            </ul>
            <h2>Süreç nasıl işliyor?</h2>
            <p>Parçaların yüzeyi boyaya hazırlanır, boya uygulanır ve parçalar fırında kürlenir. Son aşamada yüzey görsel olarak ve kalite planına göre kontrol edilir, ardından parçalar paketlenerek sevkiyata hazırlanır.</p>
            <h2>Size sağladığı fayda</h2>
            <p>Boya işleminin kendi tesisimizde yapılması, parçayı dış tedarikçiye gönderme ihtiyacını ortadan kaldırır. Böylece teslim süresi kısalır, taşıma kaynaklı hasar riski azalır ve parça tek elden tamamlanmış olarak teslim edilir.</p>
            """,
        ["oven-painting"] = """
            <p>Paint is more than the first thing you see on a part: it is also the layer that protects it from its environment. In our two oven-cured paint lines, we give parts a durable, uniform finish.</p>
            <h2>Why oven-cured paint?</h2>
            <p>Oven-cured paint forms a cohesive, durable layer on the surface of the part. For parts working in heavy-duty conditions, that durability matters as much for service life as it does for appearance.</p>
            <ul>
            <li>Two oven-cured paint lines</li>
            <li>A durable, uniform finish</li>
            <li>Final finishing after cutting, bending and machining</li>
            <li>Planning integrated with the production line</li>
            </ul>
            <h2>How the process works</h2>
            <p>Part surfaces are prepared for painting, the paint is applied and the parts are cured in the oven. The finish is then checked visually and against the quality plan before parts are packed for shipment.</p>
            <h2>What it means for you</h2>
            <p>Painting in-house removes the need to send parts to an outside supplier. That shortens lead times, reduces the risk of transport damage and means parts arrive finished from a single source.</p>
            """,

        ["kaplama"] = """
            <p>Zorlu koşullarda çalışan bir parçanın ömrünü çoğu zaman yüzeyi belirler. Yüzey kaplama çözümlerimizle parçaların korozyon ve aşınma direncini artırıyoruz.</p>
            <h2>Kaplama neden önemli?</h2>
            <p>Nem, kimyasal temas ve sürtünme, parçanın yüzeyinden başlayarak performansını düşürür. Doğru seçilmiş bir kaplama, parçanın ana malzemesini bu etkilerden korur ve bakım aralıklarının uzamasına katkı sağlar.</p>
            <ul>
            <li>Korozyon direncini artıran uygulamalar</li>
            <li>Aşınma direncini artıran uygulamalar</li>
            <li>Parçanın kullanım koşuluna göre kaplama seçimi</li>
            <li>İşleme ve kalite kontrolle entegre süreç</li>
            </ul>
            <h2>Süreç nasıl işliyor?</h2>
            <p>Parçanın çalışacağı ortam ve beklenen ömür değerlendirilerek uygun kaplama belirlenir. Yüzey hazırlığının ardından kaplama uygulanır ve parçalar, sonrasında boyutsal ve fiziksel testlerden geçirilir.</p>
            <h2>Size sağladığı fayda</h2>
            <p>Parçanın çalışma koşuluna uygun bir yüzey, sahada beklenmedik duruşların ve erken değişimlerin önüne geçmeye yardımcı olur. İhtiyacınızı paylaşın, parçanız için uygun çözümü birlikte belirleyelim.</p>
            """,
        ["surface-coating"] = """
            <p>The service life of a part working in harsh conditions is often decided by its surface. Our surface coating solutions improve the corrosion and wear resistance of parts.</p>
            <h2>Why coating matters</h2>
            <p>Moisture, chemical contact and friction degrade a part's performance starting from its surface. A well-chosen coating protects the base material from these effects and helps extend maintenance intervals.</p>
            <ul>
            <li>Treatments that improve corrosion resistance</li>
            <li>Treatments that improve wear resistance</li>
            <li>Coating selection based on operating conditions</li>
            <li>A process integrated with machining and quality control</li>
            </ul>
            <h2>How the process works</h2>
            <p>The right coating is chosen by assessing the part's working environment and expected life. After surface preparation the coating is applied, and parts then go through dimensional and physical tests.</p>
            <h2>What it means for you</h2>
            <p>A surface suited to the working conditions helps prevent unexpected downtime and early replacement in the field. Share your requirements and we will work out the right solution for your part together.</p>
            """,

        ["celik-dokum"] = """
            <p>İş makinalarında darbeye ve aşınmaya en çok maruz kalan parçaların dayanımı, dökümle başlar. Kendi çelik döküm tesisimizde alaşımlı ve karbon çeliği döküm parçalar üretiyoruz.</p>
            <h2>Kendi dökümhanemizin farkı</h2>
            <p>Dökümü dışarıdan almak yerine kendi tesisimizde yapmak, malzemeden nihai parçaya kadar süreci kontrol etmemizi sağlar. Döküm parçalar aynı çatı altında işlenir, yüzey işlemlerinden geçirilir ve kalite kontrol sonrası sevk edilir.</p>
            <ul>
            <li>Alaşımlı çelik döküm parçalar</li>
            <li>Karbon çeliği döküm parçalar</li>
            <li>CNC işleme ile entegre üretim</li>
            <li>Teknik resme veya numuneye göre üretim</li>
            </ul>
            <h2>Süreç nasıl işliyor?</h2>
            <p>Parçanın geometrisine göre döküm yöntemi ve kalıp planlanır. Ergitilen çelik kalıba dökülür, soğuyan parça temizlenir ve gerekli işlemlerden geçirilir. Parçalar, ihtiyaca göre CNC hatlarımızda nihai ölçülerine getirilir ve kontrol edilir.</p>
            <h2>Size sağladığı fayda</h2>
            <p>Dökümden son işlemeye tüm adımların tek tesiste olması, tedarik zincirini kısaltır ve izlenebilirliği artırır. Yedek parça numunesi olan işlerde de tersine mühendislik desteğimizle parçayı yeniden üretilebilir hale getirebiliriz.</p>
            """,
        ["steel-casting"] = """
            <p>The strength of the parts most exposed to impact and wear on heavy machinery begins with casting. We produce alloy and carbon steel castings in our own foundry.</p>
            <h2>The difference of our own foundry</h2>
            <p>Casting in-house rather than buying in lets us control the process from material to finished part. Castings are machined under the same roof, go through surface finishing and are shipped after quality control.</p>
            <ul>
            <li>Alloy steel castings</li>
            <li>Carbon steel castings</li>
            <li>Production integrated with CNC machining</li>
            <li>Production from a technical drawing or sample</li>
            </ul>
            <h2>How the process works</h2>
            <p>The casting method and mould are planned around the geometry of the part. Molten steel is poured into the mould, and the cooled part is cleaned and processed as needed. Parts are then brought to final dimensions on our CNC lines where required, and inspected.</p>
            <h2>What it means for you</h2>
            <p>Having every step from casting to final machining in one facility shortens the supply chain and improves traceability. Where only a sample of a spare part exists, our reverse engineering support can make it possible to produce it again.</p>
            """,

        ["tasarim-ve-muhendislik"] = """
            <p>İyi bir parça, üretimden önce doğru tasarımla başlar. 3D modelleme, tersine mühendislik ve kalıp tasarımı çalışmalarımızla bir fikri ya da eski bir numuneyi üretilebilir bir parçaya dönüştürüyoruz.</p>
            <h2>Neler yapıyoruz?</h2>
            <ul>
            <li><strong>3D modelleme:</strong> Parçanın dijital modelinin oluşturulması ve üretim öncesi değerlendirilmesi</li>
            <li><strong>Tersine mühendislik:</strong> Çizimi bulunmayan parçaların numuneden yola çıkarak yeniden tanımlanması</li>
            <li><strong>Kalıp tasarımı:</strong> Döküm ve kauçuk kalıplama için parçaya özel kalıpların tasarlanması</li>
            <li><strong>Üretilebilirlik değerlendirmesi:</strong> Tasarımın üretim yöntemine uygun hale getirilmesi</li>
            </ul>
            <h2>Çizimi olmayan bir parçanız mı var?</h2>
            <p>Üretimi durmuş veya teknik resmi bulunamayan yedek parçalar, özellikle ağır hizmet makinelerinde sık karşılaşılan bir sorundur. Numuneyi bize ulaştırmanız halinde parçayı inceleyip ölçerek yeniden üretim için tanımlayabiliriz.</p>
            <h2>Süreç nasıl işliyor?</h2>
            <p>Teknik resminizi, numunenizi veya ihtiyacınızı bizimle paylaşırsınız. Mühendislik ekibimiz parçayı değerlendirir, modeli ve gerekli kalıbı hazırlar. Numune üretimi ve onayın ardından seri üretime geçilir.</p>
            <h2>Size sağladığı fayda</h2>
            <p>Mühendislik ekibimizin üretim hatlarıyla aynı çatı altında çalışması, tasarımın baştan üretim gerçeklerine göre şekillenmesini sağlar; deneme ve revizyon döngüleri kısalır.</p>
            """,
        ["design-and-engineering"] = """
            <p>A good part starts with the right design, before production begins. Through 3D modelling, reverse engineering and tool design, we turn an idea or an old sample into a part that can be manufactured.</p>
            <h2>What we do</h2>
            <ul>
            <li><strong>3D modelling:</strong> Creating a digital model of the part and assessing it before production</li>
            <li><strong>Reverse engineering:</strong> Redefining parts with no drawing, working from a sample</li>
            <li><strong>Tool design:</strong> Designing part-specific tools for casting and rubber moulding</li>
            <li><strong>Manufacturability review:</strong> Adapting the design to the production method</li>
            </ul>
            <h2>Have a part with no drawing?</h2>
            <p>Spare parts that are out of production or have no surviving drawing are a common problem, especially on heavy-duty machinery. Send us a sample and we can examine and measure it, then define the part for production again.</p>
            <h2>How the process works</h2>
            <p>You share your drawing, sample or requirement. Our engineering team assesses the part and prepares the model and any tooling needed. Series production follows sample production and approval.</p>
            <h2>What it means for you</h2>
            <p>With our engineers working under the same roof as the production lines, designs are shaped by manufacturing realities from the start, which shortens trial and revision cycles.</p>
            """,

        ["kalite-kontrol"] = """
            <p>Kalite, son aşamada yapılan bir kontrol değil, üretimin her adımına yayılan bir disiplindir. Hammaddeden sevkiyata kadar her aşamada ölçüm, test ve izlenebilirlik sağlıyoruz.</p>
            <h2>Her aşamada kontrol</h2>
            <ul>
            <li><strong>Hammadde girişi:</strong> Gelen malzemenin üretime alınmadan önce kontrol edilmesi</li>
            <li><strong>Üretim sırasında:</strong> İlk parça onayı ve kritik ölçülerin süreç içinde takibi</li>
            <li><strong>Yüzey işlem sonrası:</strong> Boya ve kaplama sonrası boyutsal ve fiziksel testler</li>
            <li><strong>Sevkiyat öncesi:</strong> Son kontrol ve ihracata uygun paketleme</li>
            </ul>
            <h2>İzlenebilirlik</h2>
            <p>Bir parçanın hangi malzemeden, hangi aşamalardan geçerek üretildiğinin takip edilebilmesi, olası bir sorunun kaynağına hızla inmeyi sağlar. Ölçülebilir kayıtlar, aynı kalitenin bir sonraki üretimde de korunmasına yardımcı olur.</p>
            <h2>Neden önemli?</h2>
            <p>İş makinası ve savunma sanayii gibi alanlarda bir parçanın beklenen performansı gösterememesi, duruş ve maliyet anlamına gelir. Şartnameye tam uyum yaklaşımımızın temelinde bu sorumluluk vardır.</p>
            <h2>Size sağladığı fayda</h2>
            <p>Üretimin tüm adımları tek tesiste olduğu için kaliteyi yalnızca son üründe değil, sürecin tamamında takip edebiliyoruz. Parçanızla ilgili kalite beklentilerinizi teklif aşamasında bizimle paylaşabilirsiniz.</p>
            """,
        ["quality-control"] = """
            <p>Quality is not a check made at the end; it is a discipline that runs through every step of production. We provide measurement, testing and traceability at every stage, from raw material to shipment.</p>
            <h2>Control at every stage</h2>
            <ul>
            <li><strong>Incoming material:</strong> Checking materials before they enter production</li>
            <li><strong>During production:</strong> First-part approval and in-process monitoring of critical dimensions</li>
            <li><strong>After surface finishing:</strong> Dimensional and physical tests after painting and coating</li>
            <li><strong>Before shipment:</strong> Final inspection and packaging suitable for export</li>
            </ul>
            <h2>Traceability</h2>
            <p>Being able to trace what material a part was made from and which stages it went through allows a fast route to the source of any problem. Measurable records also help keep the same quality in the next production run.</p>
            <h2>Why it matters</h2>
            <p>In fields such as heavy machinery and defence, a part that falls short of expected performance means downtime and cost. That responsibility is the basis of our approach of full compliance with specifications.</p>
            <h2>What it means for you</h2>
            <p>Because every production step is in one facility, we can follow quality across the whole process, not only in the finished product. You are welcome to share your quality expectations for your part at the quotation stage.</p>
            """
    };
}
