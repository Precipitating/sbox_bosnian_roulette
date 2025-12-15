using Sandbox;
using Sandbox.Rendering;
using System;


/// <summary>
/// Simulation of the vertical scrolling - horizontal "tracking" effect on old VHS tapes.
/// </summary>

[Title( "VHS Distortion" )]
[Category( "Post Processing" )]
[Icon( "dehaze" )]




public sealed class CCSVHSD : Component, Component.ExecuteInEditor
{




	/// <summary>
	/// How big are the distortion lines.
	/// </summary>
	[Property, Title("Warp Size"), Range( 0, 20.0f)]  
    public float warp_size { get; set; } = 0f;

	/// <summary>
	/// Enhance the distorted areas.
	/// </summary>
	[Property, Title("Warp Distortion Multiplier"), Range( -0.5f, 0.5f )]  
    public float warp_distort { get; set; } = 0.007f;	

	/// <summary>
	/// How fast the distorted areas scroll.
	/// </summary>
	[Property, Title("Warp Speed"), Range( -30.0f, 30.0f )]  
    public float warp_speed { get; set; } =0f;	
	
	/// <summary>
	/// number of smaller lines to chop the main warp lines into.
	/// </summary>
	[Property, Title("Warp Variation"), Range( -30.0f, 30.0f )]  
    public float warp_random { get; set; } = 0f;	



	/// <summary>
	/// How fast the distorted areas scroll.
	/// </summary>
	[Property, Title("Chromatic Abberation"), Range( -0.25f, 0.25f )]  
    public float ca { get; set; } = -0.02f;	
	
	/// <summary>
	/// Amount of static that each warp line has
	/// </summary>
	[Property, Title("Static Amount"), Range( 0.0f, 1.0f)]  
    public float Static { get; set; } = 0f;
	

	/// <summary>
	/// high frequency horizontal ripple distortion effect on the entire image.
	/// </summary>
	[Property, Title("DeInterlace Skew"), Range( 0.0f, 20.0f )]  
    public float dSkew { get; set; } = 1f;


	CameraComponent cc = null;
	CommandList commands = null;

	protected override void OnEnabled()
    {
		commands = new CommandList( "CSSVHSD" );
		cc = Components.Get<CameraComponent>( true );
		RenderEffect();
		cc.AddCommandList( commands, Stage.AfterUI );
		
    }
	
    protected override void OnDisabled()
    {

		cc.RemoveCommandList( commands );
		cc = null;
		commands = null;
    }

    RenderAttributes attributes = new RenderAttributes();

	
    public void RenderEffect( )
    {
        if ( !cc.EnablePostProcessing )
            return;
		attributes.Set( "warp_size", warp_size );
		attributes.Set( "warp_speed", warp_speed );
		attributes.Set( "warp_random", warp_random );
		attributes.Set( "warp_distort", warp_distort );
		attributes.Set( "ca", ca );
		attributes.Set( "Static", Static );
		attributes.Set( "dSkew", dSkew);
		commands.Attributes.GrabFrameTexture( "ColorBuffer");
       // Graphics.GrabDepthTexture( "DepthBuffer", attributes );
        commands.Blit( Material.Load( "materials/postprocess/ccs_vhsd.vmat" ), attributes );

    }
}
