using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace CanardSharp.Dsdl.Testing.Serialization.AutogeneratedTests
{
    [TestClass]
    public sealed class Tests
    {
        [TestMethod]
        public void S00_t_uint4_0()
        {
            var obj = new S00_t_uint4 { val = 0 };
            SerializationTestEngine.Test(obj, "00", doRoundtripTest: true);
        }

        [TestMethod]
        public void S00_t_uint4_1()
        {
            var obj = new S00_t_uint4 { val = 1 };
            SerializationTestEngine.Test(obj, "10", doRoundtripTest: true);
        }

        [TestMethod]
        public void S00_t_uint4_2()
        {
            var obj = new S00_t_uint4 { val = 15 };
            SerializationTestEngine.Test(obj, "f0", doRoundtripTest: true);
        }

        [TestMethod]
        public void S00_t_uint4_3()
        {
            var obj = new S00_t_uint4 { val = 248 };
            SerializationTestEngine.Test(obj, "80", doRoundtripTest: false);
        }

        [TestMethod]
        public void S00_t_uint4_4()
        {
            var obj = new S00_t_uint4 { val = 255 };
            SerializationTestEngine.Test(obj, "f0", doRoundtripTest: false);
        }

        [TestMethod]
        public void S01_s_uint4_5()
        {
            var obj = new S01_s_uint4 { val = 0 };
            SerializationTestEngine.Test(obj, "00", doRoundtripTest: true);
        }

        [TestMethod]
        public void S01_s_uint4_6()
        {
            var obj = new S01_s_uint4 { val = 1 };
            SerializationTestEngine.Test(obj, "10", doRoundtripTest: true);
        }

        [TestMethod]
        public void S01_s_uint4_7()
        {
            var obj = new S01_s_uint4 { val = 15 };
            SerializationTestEngine.Test(obj, "f0", doRoundtripTest: true);
        }

        [TestMethod]
        public void S01_s_uint4_8()
        {
            var obj = new S01_s_uint4 { val = 248 };
            SerializationTestEngine.Test(obj, "f0", doRoundtripTest: false);
        }

        [TestMethod]
        public void S01_s_uint4_9()
        {
            var obj = new S01_s_uint4 { val = 255 };
            SerializationTestEngine.Test(obj, "f0", doRoundtripTest: false);
        }

        [TestMethod]
        public void S02_uint64_10()
        {
            var obj = new S02_uint64 { val = 0 };
            SerializationTestEngine.Test(obj, "0000000000000000", doRoundtripTest: true);
        }

        [TestMethod]
        public void S02_uint64_11()
        {
            var obj = new S02_uint64 { val = 1 };
            SerializationTestEngine.Test(obj, "0100000000000000", doRoundtripTest: true);
        }

        [TestMethod]
        public void S02_uint64_12()
        {
            var obj = new S02_uint64 { val = 18446744073709551615 };
            SerializationTestEngine.Test(obj, "ffffffffffffffff", doRoundtripTest: true);
        }

        [TestMethod]
        public void S03_int64_13()
        {
            var obj = new S03_int64 { val = 0 };
            SerializationTestEngine.Test(obj, "0000000000000000", doRoundtripTest: true);
        }

        [TestMethod]
        public void S03_int64_14()
        {
            var obj = new S03_int64 { val = 1 };
            SerializationTestEngine.Test(obj, "0100000000000000", doRoundtripTest: true);
        }

        [TestMethod]
        public void S03_int64_15()
        {
            var obj = new S03_int64 { val = -1 };
            SerializationTestEngine.Test(obj, "ffffffffffffffff", doRoundtripTest: true);
        }

        [TestMethod]
        public void S03_int64_16()
        {
            var obj = new S03_int64 { val = -150 };
            SerializationTestEngine.Test(obj, "6affffffffffffff", doRoundtripTest: true);
        }

        [TestMethod]
        public void S03_int64_17()
        {
            var obj = new S03_int64 { val = -9223372036854775808 };
            SerializationTestEngine.Test(obj, "0000000000000080", doRoundtripTest: true);
        }

        [TestMethod]
        public void S03_int64_18()
        {
            var obj = new S03_int64 { val = 9223372036854775807 };
            SerializationTestEngine.Test(obj, "ffffffffffffff7f", doRoundtripTest: true);
        }

        [TestMethod]
        public void S04_bool_19()
        {
            var obj = new S04_bool { val = true };
            SerializationTestEngine.Test(obj, "80", doRoundtripTest: true);
        }

        [TestMethod]
        public void S04_bool_20()
        {
            var obj = new S04_bool { val = false };
            SerializationTestEngine.Test(obj, "00", doRoundtripTest: true);
        }

        [TestMethod]
        public void S05_float16_21()
        {
            var obj = new S05_float16 { val = 0 };
            SerializationTestEngine.Test(obj, "0000", doRoundtripTest: true);
        }

        [TestMethod]
        public void S05_float16_22()
        {
            var obj = new S05_float16 { val = 1 };
            SerializationTestEngine.Test(obj, "003c", doRoundtripTest: true);
        }

        [TestMethod]
        public void S05_float16_23()
        {
            var obj = new S05_float16 { val = -1 };
            SerializationTestEngine.Test(obj, "00bc", doRoundtripTest: true);
        }

        [TestMethod]
        public void S05_float16_24()
        {
            var obj = new S05_float16 { val = 1.2E+10f };
            SerializationTestEngine.Test(obj, "ff7b", doRoundtripTest: false);
        }

        [TestMethod]
        public void S05_float16_25()
        {
            var obj = new S05_float16 { val = 1.2E+2f };
            SerializationTestEngine.Test(obj, "8057", doRoundtripTest: true);
        }

        [TestMethod]
        public void S05_float16_26()
        {
            var obj = new S05_float16 { val = 1.2E-2f };
            SerializationTestEngine.Test(obj, "2522", doRoundtripTest: true);
        }

        [TestMethod]
        public void S06_float64_27()
        {
            var obj = new S06_float64 { val = 0 };
            SerializationTestEngine.Test(obj, "0000000000000000", doRoundtripTest: true);
        }

        [TestMethod]
        public void S06_float64_28()
        {
            var obj = new S06_float64 { val = 1.7976931348623157E+308 };
            SerializationTestEngine.Test(obj, "ffffffffffffef7f", doRoundtripTest: true);
        }

        [TestMethod]
        public void S06_float64_29()
        {
            var obj = new S06_float64 { val = -1.7976931348623157E+308 };
            SerializationTestEngine.Test(obj, "ffffffffffffefff", doRoundtripTest: true);
        }

        [TestMethod]
        public void S06_float64_30()
        {
            var obj = new S06_float64 { val = 0.1 };
            SerializationTestEngine.Test(obj, "9a9999999999b93f", doRoundtripTest: true);
        }

        [TestMethod]
        public void S07_as4_uint8_31()
        {
            var obj = new S07_as4_uint8 { val = new byte[] { 0, 0, 0, 0 } };
            SerializationTestEngine.Test(obj, "00000000", doRoundtripTest: true);
        }

        [TestMethod]
        public void S07_as4_uint8_32()
        {
            var obj = new S07_as4_uint8 { val = new byte[] { 0, 1, 2, 3 } };
            SerializationTestEngine.Test(obj, "00010203", doRoundtripTest: true);
        }

        [TestMethod]
        public void S07_as4_uint8_33()
        {
            var obj = new S07_as4_uint8 { val = new byte[] { 255, 255, 255, 255 } };
            SerializationTestEngine.Test(obj, "ffffffff", doRoundtripTest: true);
        }

        [TestMethod]
        public void S08_ad4_uint8_34()
        {
            var obj = new S08_ad4_uint8 { val = new byte[] {  } };
            SerializationTestEngine.Test(obj, "", doRoundtripTest: true);
        }

        [TestMethod]
        public void S08_ad4_uint8_35()
        {
            var obj = new S08_ad4_uint8 { val = new byte[] { 0 } };
            SerializationTestEngine.Test(obj, "00", doRoundtripTest: true);
        }

        [TestMethod]
        public void S08_ad4_uint8_36()
        {
            var obj = new S08_ad4_uint8 { val = new byte[] { 0, 0 } };
            SerializationTestEngine.Test(obj, "0000", doRoundtripTest: true);
        }

        [TestMethod]
        public void S08_ad4_uint8_37()
        {
            var obj = new S08_ad4_uint8 { val = new byte[] { 0, 0, 0 } };
            SerializationTestEngine.Test(obj, "000000", doRoundtripTest: true);
        }

        [TestMethod]
        public void S08_ad4_uint8_38()
        {
            var obj = new S08_ad4_uint8 { val = new byte[] { 0, 0, 0, 0 } };
            SerializationTestEngine.Test(obj, "00000000", doRoundtripTest: true);
        }

        [TestMethod]
        public void S08_ad4_uint8_39()
        {
            var obj = new S08_ad4_uint8 { val = new byte[] { 1 } };
            SerializationTestEngine.Test(obj, "01", doRoundtripTest: true);
        }

        [TestMethod]
        public void S08_ad4_uint8_40()
        {
            var obj = new S08_ad4_uint8 { val = new byte[] { 1, 1 } };
            SerializationTestEngine.Test(obj, "0101", doRoundtripTest: true);
        }

        [TestMethod]
        public void S08_ad4_uint8_41()
        {
            var obj = new S08_ad4_uint8 { val = new byte[] { 1, 1, 1 } };
            SerializationTestEngine.Test(obj, "010101", doRoundtripTest: true);
        }

        [TestMethod]
        public void S08_ad4_uint8_42()
        {
            var obj = new S08_ad4_uint8 { val = new byte[] { 1, 1, 1, 1 } };
            SerializationTestEngine.Test(obj, "01010101", doRoundtripTest: true);
        }

        [TestMethod]
        public void S09_as4_uint8_ad4_uint8_43()
        {
            var obj = new S09_as4_uint8_ad4_uint8 { val0 = new byte[] { 1, 1, 1, 1 }, val1 = new byte[] {  } };
            SerializationTestEngine.Test(obj, "01010101", doRoundtripTest: true);
        }

        [TestMethod]
        public void S09_as4_uint8_ad4_uint8_44()
        {
            var obj = new S09_as4_uint8_ad4_uint8 { val0 = new byte[] { 1, 1, 1, 1 }, val1 = new byte[] { 1, 1, 1, 1 } };
            SerializationTestEngine.Test(obj, "0101010101010101", doRoundtripTest: true);
        }

        [TestMethod]
        public void S10_ad4_uint8_as4_uint8_45()
        {
            var obj = new S10_ad4_uint8_as4_uint8 { val0 = new byte[] {  }, val1 = new byte[] { 1, 1, 1, 1 } };
            SerializationTestEngine.Test(obj, "0020202020", doRoundtripTest: true);
        }

        [TestMethod]
        public void S10_ad4_uint8_as4_uint8_46()
        {
            var obj = new S10_ad4_uint8_as4_uint8 { val0 = new byte[] { 1, 1, 1, 1 }, val1 = new byte[] { 1, 1, 1, 1 } };
            SerializationTestEngine.Test(obj, "802020202020202020", doRoundtripTest: true);
        }

        [TestMethod]
        public void S11_s_int24_47()
        {
            var obj = new S11_s_int24 { val = 0 };
            SerializationTestEngine.Test(obj, "000000", doRoundtripTest: true);
        }

        [TestMethod]
        public void S11_s_int24_48()
        {
            var obj = new S11_s_int24 { val = 4194304 };
            SerializationTestEngine.Test(obj, "000040", doRoundtripTest: true);
        }

        [TestMethod]
        public void S11_s_int24_49()
        {
            var obj = new S11_s_int24 { val = -2097152 };
            SerializationTestEngine.Test(obj, "0000e0", doRoundtripTest: true);
        }

        [TestMethod]
        public void S11_s_int24_50()
        {
            var obj = new S11_s_int24 { val = 134217743 };
            SerializationTestEngine.Test(obj, "ffff7f", doRoundtripTest: false);
        }

        [TestMethod]
        public void S11_s_int24_51()
        {
            var obj = new S11_s_int24 { val = -134217744 };
            SerializationTestEngine.Test(obj, "000080", doRoundtripTest: false);
        }

        [TestMethod]
        public void S12_s_uint24_52()
        {
            var obj = new S12_s_uint24 { val = 0 };
            SerializationTestEngine.Test(obj, "000000", doRoundtripTest: true);
        }

        [TestMethod]
        public void S12_s_uint24_53()
        {
            var obj = new S12_s_uint24 { val = 4194304 };
            SerializationTestEngine.Test(obj, "000040", doRoundtripTest: true);
        }

        [TestMethod]
        public void S12_s_uint24_54()
        {
            var obj = new S12_s_uint24 { val = 2097152 };
            SerializationTestEngine.Test(obj, "000020", doRoundtripTest: true);
        }

        [TestMethod]
        public void S12_s_uint24_55()
        {
            var obj = new S12_s_uint24 { val = 134217743 };
            SerializationTestEngine.Test(obj, "ffffff", doRoundtripTest: false);
        }

        [TestMethod]
        public void S12_s_uint24_56()
        {
            var obj = new S12_s_uint24 { val = 134217744 };
            SerializationTestEngine.Test(obj, "ffffff", doRoundtripTest: false);
        }

        [TestMethod]
        public void S14_s_uint24_57()
        {
            var obj = new S14_s_uint24 { val = 0 };
            SerializationTestEngine.Test(obj, "000000", doRoundtripTest: true);
        }

        [TestMethod]
        public void S14_s_uint24_58()
        {
            var obj = new S14_s_uint24 { val = 4194304 };
            SerializationTestEngine.Test(obj, "000040", doRoundtripTest: true);
        }

        [TestMethod]
        public void S14_s_uint24_59()
        {
            var obj = new S14_s_uint24 { val = 2097152 };
            SerializationTestEngine.Test(obj, "000020", doRoundtripTest: true);
        }

        [TestMethod]
        public void S14_s_uint24_60()
        {
            var obj = new S14_s_uint24 { val = 134217743 };
            SerializationTestEngine.Test(obj, "0f0000", doRoundtripTest: false);
        }

        [TestMethod]
        public void S14_s_uint24_61()
        {
            var obj = new S14_s_uint24 { val = 134217744 };
            SerializationTestEngine.Test(obj, "100000", doRoundtripTest: false);
        }

        [TestMethod]
        public void CmpTestA_A_62()
        {
            var obj = new CmpTestA_A { field = 123, a = new CmpTestA { field = 7 } };
            SerializationTestEngine.Test(obj, "7b07", doRoundtripTest: true);
        }

        [TestMethod]
        public void TAO1_63()
        {
            var obj = new TAO1 { foo = 0, array = new byte[] {  } };
            SerializationTestEngine.Test(obj, "00", doRoundtripTest: true);
        }

        [TestMethod]
        public void TAO1_64()
        {
            var obj = new TAO1 { foo = 0, array = new byte[] { 1, 2, 3, 4, 5, 6, 7, 8 } };
            SerializationTestEngine.Test(obj, "000102030405060708", doRoundtripTest: true);
        }

        [TestMethod]
        public void TAO2_65()
        {
            var obj = new TAO2 { foo = 32, array = new byte[] {  } };
            SerializationTestEngine.Test(obj, "005000", doRoundtripTest: true);
        }

        [TestMethod]
        public void TAO2_66()
        {
            var obj = new TAO2 { foo = 32, array = new byte[] { 1, 2, 3, 4 } };
            SerializationTestEngine.Test(obj, "005040208184", doRoundtripTest: true);
        }

        [TestMethod]
        public void TAO3_67()
        {
            var obj = new TAO3 { array = new byte[] { 2, 3 }, bar = 213 };
            SerializationTestEngine.Test(obj, "20203a85a0", doRoundtripTest: true);
        }

        [TestMethod]
        public void TAO4_68()
        {
            var obj = new TAO4 { array = new bool[] { true, false } };
            SerializationTestEngine.Test(obj, "0a", doRoundtripTest: true);
        }

        [TestMethod]
        public void TAO5_69()
        {
            var obj = new TAO5 { array = new TAO4[] { new TAO4 { array = new bool[] { true } }, new TAO4 { array = new bool[] {  } }, new TAO4 { array = new bool[] { true, true, false } } } };
            SerializationTestEngine.Test(obj, "0c1801e0", doRoundtripTest: true);
        }

        [TestMethod]
        public void TAO6_70()
        {
            var obj = new TAO6 { array = new TAO1[] { new TAO1 { foo = 123, array = new byte[] { 7 } } } };
            SerializationTestEngine.Test(obj, "7b1070", doRoundtripTest: true);
        }

        [TestMethod]
        public void TAO7_71()
        {
            var obj = new TAO7 { array = new TAO1[] { new TAO1 { foo = 123, array = new byte[] { 7 } } }, baz = 123 };
            SerializationTestEngine.Test(obj, "5ec41ec15c", doRoundtripTest: true);
        }

        [TestMethod]
        public void TAO8_72()
        {
            var obj = new TAO8 { fooz = 2, array = new double[] { 1.1, 1.2, 1.3 } };
            SerializationTestEngine.Test(obj, "29a9999999999f13f333333333333f33fcdccccccccccf43f0", doRoundtripTest: true);
        }

        [TestMethod]
        public void TAO9_73()
        {
            var obj = new TAO9 { array = new TAO8[] { new TAO8 { fooz = 2, array = new double[] { 1.1, 1.2, 1.3 } }, new TAO8 { fooz = 2, array = new double[] { 1.1, 1.2, 1.3 } }, new TAO8 { fooz = 2, array = new double[] { 1.1, 1.2, 1.3 } } } };
            SerializationTestEngine.Test(obj, "3207353333333333e27e666666666667e67f9b9999999999e87e40e6a6666666667c4fccccccccccccfccff373333333333d0fca6a6666666667c4fccccccccccccfccff373333333333d0fc", doRoundtripTest: true);
        }

        [TestMethod]
        public void Union1_74()
        {
            var obj = new Union1 { b = 25 };
            SerializationTestEngine.Test(obj, "4640", doRoundtripTest: true);
        }

        [TestMethod]
        public void Union1_75()
        {
            var obj = new Union1 { a = 25 };
            SerializationTestEngine.Test(obj, "064000", doRoundtripTest: true);
        }

        [TestMethod]
        public void Union1_76()
        {
            var obj = new Union1 { c = 1.2 };
            SerializationTestEngine.Test(obj, "8cccccccccccfccfc0", doRoundtripTest: true);
        }

        [TestMethod]
        public void Void1_77()
        {
            var obj = new Void1 { i = 12 };
            SerializationTestEngine.Test(obj, "060000", doRoundtripTest: true);
        }

    }
}