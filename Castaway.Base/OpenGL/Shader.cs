using System;
using System.Linq;
using Castaway.Native.GL;
using Castaway.Rendering;

namespace Castaway.OpenGL
{
    public sealed class Shader : ShaderObject
    {
        public bool Destroyed { get; set; }
        public uint Number { get; set; }
        public override string Name => $"{Number}({Valid})";
        public override bool Valid => GL.IsProgram(Number) && !Destroyed;
        public ShaderInputBinder? Binder;
        
        public override void Bind()
        {
            if (Graphics.Current is not OpenGL32 gl) throw new InvalidOperationException("Need OpenGL >= 3.2");
            gl.BindShader(Number);
            Graphics.Current.BoundShader = this;
        }

        public override void Unbind()
        {
            if (Graphics.Current is not OpenGL32 gl) throw new InvalidOperationException("Need OpenGL >= 3.2");
            gl.UnbindShader();
            Graphics.Current.BoundShader = null;
        }

        public bool LinkSuccess => GL.GetProgram(Number, GL.ProgramQuery.LinkStatus) == 1;

        public string LinkLog
        {
            get
            {
                GL.GetProgramInfoLog(Number, out _, out var ret);
                return ret;
            }
        }

        public Shader(params SeparatedShaderObject[] shaders) : base(shaders)
        {
            Number = GL.CreateProgram();
            foreach (var separatedShaderObject in shaders)
            {
                if (separatedShaderObject is not ShaderPart s) continue;
                GL.AttachShader(Number, s.Number);
                s.Dispose();
            }
        }

        public override void Dispose()
        {
            GL.DeleteProgram(Number);
        }

        public override void Link()
        {
            foreach (var o in GetOutputs())
            {
                var c = GetOutput(o);
                GL.BindFragDataLocation(Number, c, o);
            }
            GL.LinkProgram(Number);
            var log = LinkLog;
            if(log.Any()) Console.Error.WriteLine(log);
            if (!LinkSuccess) throw new GraphicsException("Failed to link program.");
            Binder = new ShaderInputBinder(this);
        }
    }
}