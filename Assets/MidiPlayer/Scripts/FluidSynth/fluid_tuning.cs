namespace MidiPlayerTK
{
    public class fluid_tuning
    {
        public int bank;
        public string name;
        public float[] pitch; //[128];  /* the pitch of every key, in cents */
        public int prog;

        //#define fluid_tuning_get_bank(_t) ((_t)->bank)
        //#define fluid_tuning_get_prog(_t) ((_t)->prog)
        //#define fluid_tuning_get_pitch(_t, _key) ((_t)->pitch[_key])
        //#define fluid_tuning_get_all(_t) (&(_t)->pitch[0])


        public fluid_tuning(string pname, int pbank, int pprog)
        {
            name = pname;
            bank = pbank;
            prog = pprog;
            pitch = new float[128];
            for (var i = 0; i < 128; i++)
                pitch[i] = i * 100.0f;
        }

        private void fluid_tuning_set_name(string pname)
        {
            name = pname;
        }

        private static string fluid_tuning_get_name(fluid_tuning tuning)
        {
            return tuning.name;
        }

        private void fluid_tuning_set_key(int key, float ppitch)
        {
            pitch[key] = ppitch;
        }

        private void fluid_tuning_set_octave(float[] pitch_deriv)
        {
            for (var i = 0; i < 128; i++) pitch[i] = i * 100.0f + pitch_deriv[i % 12];
        }

        private void fluid_tuning_set_all(float[] ppitch)
        {
            int i;

            for (i = 0; i < 128; i++) pitch[i] = ppitch[i];
        }

        private void fluid_tuning_set_pitch(int key, float ppitch)
        {
            if (key >= 0 && key < 128) pitch[key] = ppitch;
        }
    }
}