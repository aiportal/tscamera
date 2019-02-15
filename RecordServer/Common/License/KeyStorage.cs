using System;
using System.Collections.Generic;
using System.Text;

namespace bfbd.Common.License
{
	sealed partial class KeyStorage
	{
		public static RsaKey GenerateKey()
		{
			var rcp = new System.Security.Cryptography.RSACryptoServiceProvider();
			return new RsaKey() { PrivateKey = rcp.ToXmlString(true), PublicKey = rcp.ToXmlString(false) };
		}

		public static string GetPublicKey(string keyName)
		{
			keyName = string.IsNullOrEmpty(keyName) ? "0" : keyName;
			if (_keys.ContainsKey(keyName))
				return _keys[keyName].PublicKey;
			else
				throw new ArgumentOutOfRangeException("keyName");
		}

		public static string GetPrivateKey(string keyName)
		{
			keyName = string.IsNullOrEmpty(keyName) ? "0" : keyName;
			if (_keys.ContainsKey(keyName))
				return _keys[keyName].PrivateKey;
			else
				throw new ArgumentOutOfRangeException("keyName");
		}
	}

	partial class KeyStorage
	{
		private static readonly Dictionary<string, RsaKey> _keys = new Dictionary<string, RsaKey>()
		{
			{"0", new RsaKey(){
				PublicKey = "<RSAKeyValue><Modulus>oSfNzKzK5FB+0mcR23VXR0X9w9MbLtw7T9CTPHRgSBEPrN/xoYY9TIyX1oAAIpfzdeNEd/JIgMFShQMvmZSBAgA5HLA8uUkH5nWYBbl4X1LKBaLv0740tJ8ZYd4OuZAtJAqYe9GKpNU3aC+b3T9uQTPR3a9ENn8axgS5/4jYQ3c=</Modulus><Exponent>AQAB</Exponent></RSAKeyValue>",
#if LicenseTool
				PrivateKey = "<RSAKeyValue><Modulus>oSfNzKzK5FB+0mcR23VXR0X9w9MbLtw7T9CTPHRgSBEPrN/xoYY9TIyX1oAAIpfzdeNEd/JIgMFShQMvmZSBAgA5HLA8uUkH5nWYBbl4X1LKBaLv0740tJ8ZYd4OuZAtJAqYe9GKpNU3aC+b3T9uQTPR3a9ENn8axgS5/4jYQ3c=</Modulus><Exponent>AQAB</Exponent><P>3k6yd8elsOC+gqKZ++2evwkC2e5jSqTUuqSnunG6Iar5IEpViE5anG7NxKwkJmLGFg+j0MK1GVytkPIDrwWGMQ==</P><Q>uZR5S7ShyvuSsJ6XpNqHrvAOMcb8OX/IiOoywcTiD2KrqX/2n+Jl8tiTCLARKEovFg2UUKGCPpPwdG19Wm9yJw==</Q><DP>F+P3M/4lpUuRzbIxwCw6KieKwebnjscGAYTebZ/5M9MO8NRsOrjI7hTOUjt5qxJkXUyS+0VhdvdJw9Duamm6kQ==</DP><DQ>JLORXYCGst8X/qgOs4KobNd65ytEPJbh8PnoOdwXHRXVfzsYajxbDzD3uzMe4YnyT/k1iNC4MmoNlukEbns15w==</DQ><InverseQ>GVleuy+wknmhCJuN/WbKPckLETsX8XBEZATF2OHr6RY2pdCl41DrBpMuYd5XqQepX+2bTTgA81M9EXFzA74Hzg==</InverseQ><D>FXAEEmwR+Vkz5a1MVNNBLvwCi+AzR4KIaU+npm0cubl7SlXoAMKouNwi/qe8XN21x0LaQtJ6Dpao8YTA+j8lAfbfJ9N2rizK1Wojvsch+g+GNGNBpWBr749/2A6N/yo+OGFYjEKM8K7VBUZujVqbfA+yWiuLVpfRJcz0LsunW8E=</D></RSAKeyValue>"
#endif
			}},
			{"Monkey", new RsaKey(){ 
				PublicKey= "<RSAKeyValue><Modulus>nl54yPkak8Bk1wD47whw32eSO1+MMO6WXukqD0r48BNZlEcqdhtEplr9X/vFOBcGD2DxkxhwEYFvtVhRd/MAT/LgV8R2WckhNTDb0loN5oOHMW2ZytJP7AcugaZWmx7Lu/8W9MlQ2wf9CvnAHhkCdsM6gTMWpzJNtT9lCqWpmy0=</Modulus><Exponent>AQAB</Exponent></RSAKeyValue>",
#if LicenseTool
				PrivateKey= "<RSAKeyValue><Modulus>nl54yPkak8Bk1wD47whw32eSO1+MMO6WXukqD0r48BNZlEcqdhtEplr9X/vFOBcGD2DxkxhwEYFvtVhRd/MAT/LgV8R2WckhNTDb0loN5oOHMW2ZytJP7AcugaZWmx7Lu/8W9MlQ2wf9CvnAHhkCdsM6gTMWpzJNtT9lCqWpmy0=</Modulus><Exponent>AQAB</Exponent><P>2m71cI+68AZGA54/bTus7N2HKOU9sP0edQYxMDXvICl3O5Hnsu2zFDqaEZNme62RCYFT4m/aAAdZV2+K0x3avw==</P><Q>uZsJppC+9bbv0AdXQuKIPwFDRykTLXkOoYV3do4r5Oj6gSPblSYcc0ECLvOChVdncefMyCUpuqpQB+DRyXBhEw==</Q><DP>0ersiP44As+knXmJLuq4pvHGILEd9mdqy7/lqZVLdzciVOdFKhlxjjE7O0TSqm0FA4N8IBKqCHkHjRWu7nC0dQ==</DP><DQ>Gs7z8+UQT/leZhPJXNXPjBool4ytQnIr1NSsTql2WZf1JtYBD2fz0AnhwNpXTd80B9XNVFNZ1aZn7NtiMdBTlQ==</DQ><InverseQ>sL67Y1s1+fLF3vseY5MJ27TkgXUKJpUzVojXhTBG1emNnS9pSvnzGYPchyU2qr9SzvFXbsgc6UvopsfUoOEfpw==</InverseQ><D>BfxSGl5356+03/90aSxrf7Yda8sPtGXAHzQ2178gq4o/r4AFFdOwA42a1/7fFGCiZQBoAQRCcBbxTuxYn+z6TPv+o/lhLoYcqHEwdI7WtWA3yDS7Cc/yoencbdEzC05tl2Xs4H8e/IlN2w57IAuZFCXIvxpJtBD4P+anQXCsrS0=</D></RSAKeyValue>"
#endif
			}},
			{"Dragon", new RsaKey(){
				PublicKey= "<RSAKeyValue><Modulus>p//0nvRk413QcM94aX84Hwzo5RjLJca6kESEXVZLZJ6PP7iwGPLv3cbiehRsdoU6txrOlACbCoHPxyyPPa7C4waR8AlXhzeoi+ZMiTLR85zlJz3qzlSwjAPDmST9x0WlDu8gQlL4VAmF1xttqYSAXSVFy9inmjg+MmYgqXnx7EM=</Modulus><Exponent>AQAB</Exponent></RSAKeyValue>",
#if LicenseTool
				PrivateKey= "<RSAKeyValue><Modulus>p//0nvRk413QcM94aX84Hwzo5RjLJca6kESEXVZLZJ6PP7iwGPLv3cbiehRsdoU6txrOlACbCoHPxyyPPa7C4waR8AlXhzeoi+ZMiTLR85zlJz3qzlSwjAPDmST9x0WlDu8gQlL4VAmF1xttqYSAXSVFy9inmjg+MmYgqXnx7EM=</Modulus><Exponent>AQAB</Exponent><P>2YuUjS58I6IfpwRS34xevTz0uqukKkRu9o+q30Wl2BAPYqTgBDu/etSGYXdjH8Fh+aQitUhAaXBC5L6aKFwn6w==</P><Q>xbJVBq3/xj3wjSz9Jm64bjqicv6ZjmVRL3nbPGLvL34+GEBK3dQfCe6gvLbz+2tKk3ag3BjGIqWT56Qe/WlPCQ==</Q><DP>ACVj2CRr2OpG8ynBlHzXo3DjCS0MUyrwmCHIj5XQYrDAEeTicZ5IpqB15qLZ4i+TDUPa8hw2TtvQb67hE53cOQ==</DP><DQ>PCviGy1cew8hJyb5SfhLJCmuhged3yxRQHz7UG7gU/k9lw4Ce2/znodF3wpkSjLGuQlTPoo/zA3PbaKTWBf0WQ==</DQ><InverseQ>agGzhQZ8mVzAvFsXuPI9vgHPY3OWLr6bHxCZSjtX1qtoHWgjRXs/I07iumdjpO31Jk+jZV9k5o4WAA6qzKLtWg==</InverseQ><D>Zici7YyZ1A71ccMV6sAqtea5rqR20k+Wnaar/b09Nr+bRxncKi9+XxJSH/1PPnogi7pHJoN/wtHQL8IOj2k2oqyb0RmaafMwFtbj6s4zD/QAtPrM56GYGFqIjI6+OUC7wYIVdFl5YAwL/cs1t7zH4LeGYxBEAANMvvYrInlsVyE=</D></RSAKeyValue>"
#endif
			}},
			{"Octopus", new RsaKey(){ 
				PublicKey= "<RSAKeyValue><Modulus>nYBuNIFLSzqj2zrQoc/rlCVyiu7JKnGQLOtxVkUYGuOLl6MnYxcGMRWBtIVii2Mhn70b40m+l5mLFicqNGP19QqPCW5RE0noyKcZ4i7qzrhnDF/1AjrytSXPdw649cgSAywOV2YoxCkBRmieOVay0SH4Jw70uX1OyEIgv4vMPVM=</Modulus><Exponent>AQAB</Exponent></RSAKeyValue>",
#if LicenseTool
				PrivateKey= "<RSAKeyValue><Modulus>nYBuNIFLSzqj2zrQoc/rlCVyiu7JKnGQLOtxVkUYGuOLl6MnYxcGMRWBtIVii2Mhn70b40m+l5mLFicqNGP19QqPCW5RE0noyKcZ4i7qzrhnDF/1AjrytSXPdw649cgSAywOV2YoxCkBRmieOVay0SH4Jw70uX1OyEIgv4vMPVM=</Modulus><Exponent>AQAB</Exponent><P>0uBvi405YzBzeg/9RYiaxVAX0VvHALTRi4Ur0UwVO8WZDwgvM2Uh6YOifSzh/csWxaHYXrgvVrfvIMH73sfDLQ==</P><Q>vzQtZRjMNKnukRECj+wDKUE1U+tkp20KXopN5ZGrkGMHVZY4YDv4wjujivv0/l0kFY+OrLFstumY/5meR6ZSfw==</Q><DP>IN8rhC13jKa42YY0jOpGdguOKuyLkOie4YjY079wb+jVeypjcTeKmcQTSD/+2nkWC6i2+czsVDBH7mtOv+OT4Q==</DP><DQ>r6pNZouA2YWiVA8vrnCketuZFg+3Owc4NqGUba0G1bHVxDEufGO86R6H46IWdTe3dkOdTlX3zpkRYwuM2sEZGw==</DQ><InverseQ>IQn6mlMV7JfnvF7Unw1WbCrjjspis9U1k1/TR6VSzdBWKHEFH0W1cGUJps6TaiFzGKgEuL7NS55qm1kkrcZJ3g==</InverseQ><D>I1eYYyvIK39jGWSfQFAZusqUY1ylw2JsScgjTIqAmDgLMOGpivC1sPn0ev+bV0/NUbzrrxrzK62GqlfrfooXvKSCRCoG4jHcTWYeeRw+C0CfOCQAsi+LsdNYh47ZX5BPlJzQH26MhpnW36LFdGaAxqvnGVsyMk7G/6cUSAnxENE=</D></RSAKeyValue>" 
#endif
			}},
			{"Whale", new RsaKey(){ 
				PublicKey= "<RSAKeyValue><Modulus>5zO2ZYQvbtjS9Vp3bjQT5mxl2eK2hStVELEThIkwEmUcF+TObxKCZehK1/wodApqEVkVpw4mEN64WfihgF7Es+U0+aIRsLEFJ/La67nwXg1yTvNOBiNuYpDr7047JrGrg7X5/sQRAIVeFYQrFbybXZ/71HMjjoI/kUZPJz7COgU=</Modulus><Exponent>AQAB</Exponent></RSAKeyValue>",
#if LicenseTool
				PrivateKey= "<RSAKeyValue><Modulus>5zO2ZYQvbtjS9Vp3bjQT5mxl2eK2hStVELEThIkwEmUcF+TObxKCZehK1/wodApqEVkVpw4mEN64WfihgF7Es+U0+aIRsLEFJ/La67nwXg1yTvNOBiNuYpDr7047JrGrg7X5/sQRAIVeFYQrFbybXZ/71HMjjoI/kUZPJz7COgU=</Modulus><Exponent>AQAB</Exponent><P>9Q5i6VpBd/JDvFDToOSGmye883Gyio0dBhazmCTSURhRVoe8swNCsGDRT2AuJ8u+L4gzG7RTZHNP5dt6JMYqUw==</P><Q>8YbwArDJwCOGwvKk/yFrvWz+FHc5g3IZtq1M+2SjvoeRNg5qZBWuAPYMnEmKgIkTV4RjIjesW/q8Kv9j/XPvRw==</Q><DP>dmQC6VYPdxF2JA6wj3SYi3EBWmO9rC5MrVpeXcy8Ry2GblWZlqqml2vO85g19Ef8lB4rAuF1wHvtR9FnC0kpvQ==</DP><DQ>hFOLt82oG27bbH+ISofDAZtvcUMI7a1bzSwRm0I+PCsGMfmf7Im8NaaYrh/UlCuFv1M3BQ4/jn1HDr+xxrD4DQ==</DQ><InverseQ>ycSllkRSqLOiBlbiyEu0Zp4KDE9OhZZZQl0rA8iO1XKgtYNOvbu6yLdvSljB+K1BRc6b2D+8fb4ezflTocJ2kw==</InverseQ><D>M2O8LA5YmLPbGCwWlck/jx0S+n45AZqjGsWxR+lSZRRSTUFPXULo9wvN7voeZ8SU1OxPMsWjUmae5WitRSJLaCDnjZ+yFH+v/L6fsDbXcz9NbP6Kh2SyRDSw95BJxLAQE69mgN7yeFoxjHr6bhz+nU9Bhjs/8RxFnO1HvqR34XU=</D></RSAKeyValue>" 
#endif
			}},
			{"5", new RsaKey(){ 
				PublicKey= "<RSAKeyValue><Modulus>pMMAAvEJh5/c9hn5QvWxFQIct9+E6Ji3jquHAcMAvujFcN022NO1nCe2ozq11zFbfYiLP1+U+4pO4GMsWFBrX/CEKVgolPEREUXe2fNtHh99XFKyUDIyUlCv0refzeYX9FKWYhKHKA11NzeNEpXuivitA6H+8e2y7U7kvuFWQgE=</Modulus><Exponent>AQAB</Exponent></RSAKeyValue>",
#if LicenseTool
				PrivateKey= "<RSAKeyValue><Modulus>pMMAAvEJh5/c9hn5QvWxFQIct9+E6Ji3jquHAcMAvujFcN022NO1nCe2ozq11zFbfYiLP1+U+4pO4GMsWFBrX/CEKVgolPEREUXe2fNtHh99XFKyUDIyUlCv0refzeYX9FKWYhKHKA11NzeNEpXuivitA6H+8e2y7U7kvuFWQgE=</Modulus><Exponent>AQAB</Exponent><P>3ErLIZbuE0Od1bx1Lb4HjZNn3MIPWe4LV9szPa+bkiK7yKDSHeNLRxG7+ST1io+MhwTNlQ/UFDF4epZ7PcKqFQ==</P><Q>v3fstLZoVN3dUTDQufp9u4WADkaghsUSp8YAGXHZ3w0s7wgcJtwdIH1n3ByL/hianBl2BqGJ5OHaQVOgT6qPPQ==</Q><DP>nGShB6y+QoDrifUsf5f8hr9SqkA/Y8oC1ZGyNCX63Wm5RTsfUMawB7mIaN2bUI6O5sA7L4s4NknUzbsNrZAI7Q==</DP><DQ>RJv4mhjy8dY+xCU15i64d0WzSWpNg32C3dO5nWSKqb7S5ySE0ff2B/poCRvBnl+6p00IWf/wpa+4wcfxnhqEEQ==</DQ><InverseQ>GXVTx2p6GtNJOC4vm6g1uhj16TApHqkkZ4wCYcqyBunNNVqDEEFkywTt/bdGNUP8JHJu1YWTOHMoi21ZjXDcwQ==</InverseQ><D>kqaSswuSfOpZBaB20hfYB6f6odtM9K9B6rZYE8RbPJH5c40jDXnbU2Ox17YhNqpTEfhgYQcVl5Myw9ziErV569uq1vpLiGzgPBHM2wZUOBnUDhxayjzf6VQgnNGJg/iCthmvMv09xyLGGAjM1A4p0iC6z8aMdJ4vFamvd4Lvj/E=</D></RSAKeyValue>" 
#endif
			}},
			{"6", new RsaKey(){ 
				PublicKey= "<RSAKeyValue><Modulus>i0AzIxFmBJdopRFz78VBG/QR7do3wFBvm4E1L0CJ1kXB9zBgwot4IjJDyG5tnxN4nfrKqQOSA+N+Tzrx8ZefJSLZ4TCaaxNvgMcNzOg6/baIj2KGOnnE8F1PVwJckPhS/vai27UPnEG1QJZ1Y6togTVHqPO/55kTXFpb8NXVVPM=</Modulus><Exponent>AQAB</Exponent></RSAKeyValue>",
#if LicenseTool
				PrivateKey= "<RSAKeyValue><Modulus>i0AzIxFmBJdopRFz78VBG/QR7do3wFBvm4E1L0CJ1kXB9zBgwot4IjJDyG5tnxN4nfrKqQOSA+N+Tzrx8ZefJSLZ4TCaaxNvgMcNzOg6/baIj2KGOnnE8F1PVwJckPhS/vai27UPnEG1QJZ1Y6togTVHqPO/55kTXFpb8NXVVPM=</Modulus><Exponent>AQAB</Exponent><P>vREAe95GevXsRjbuXyV6iX7uTfvrTx6z+chkJyR4HofhskacVAdBX06ZJzg0Vsyp2Yy+JEtt9s8v3vBuO1rEFQ==</P><Q>vIxuVKt4dNrpf0QKMBWPyqOAOq76GpVzK1LBWlS3CMyY8yqOugPjF4vhY0RLaMD4JkIgPWUs1FICxe7d/nBO5w==</Q><DP>pvKyaK6XsdAiOMYcquufTnZE25vN5umHptLqjR3I67y08QUk2ZYmhZAT8OvSW6sReEatR8NLcJr2/Hd1/vdhGQ==</DP><DQ>K3/6tG4bDFq1JDd+YjmXQxkVrHRSH2/7cjNViJGi6NTLNM7Mvv3Gltge19gZzcE3fxwidAezoAiLuz6w2NLwYQ==</DQ><InverseQ>libt9wzP5mYgvU3GOmdLRGE0lCIusNAwHcGlb6ryqUP9xKfQhgZdFgQGuy8GlFb9DT6yVlhEVHceHM8RuM8Oug==</InverseQ><D>djAfNvxlYjBAGRofFLqO8HLPZNl5yDh9AbdAtMJKOxsp7euZtIU7iYqwDUXstL+J+EwMBjeJjtNfOVAGWc8eut3A+DKg8/mniezj+mcvlhtGI0vE5QWluaOCaSHD3BnOa++eBRk8qY/672zp6kkck6aGxygA5GdHyi7Hg88MxcE=</D></RSAKeyValue>"
#endif
			}},
			{"7", new RsaKey(){ 
				PublicKey= "<RSAKeyValue><Modulus>9ffeZu8wphYvqGCzaM09Sbi6TlY0/6BvzEkE9WCt6nt76//Y/4xr54qjDYWDLSXivi6pF58PhamEqXWWr60lNsL3QMG+5ZRnm+lKjNAxjLbI6Alw5QtYGuIY87kz/WxNkS/GyGiuQQPw0RibIfO7KDbew7QfbnAq2RUYfsISuaE=</Modulus><Exponent>AQAB</Exponent></RSAKeyValue>",
#if LicenseTool
				PrivateKey= "<RSAKeyValue><Modulus>9ffeZu8wphYvqGCzaM09Sbi6TlY0/6BvzEkE9WCt6nt76//Y/4xr54qjDYWDLSXivi6pF58PhamEqXWWr60lNsL3QMG+5ZRnm+lKjNAxjLbI6Alw5QtYGuIY87kz/WxNkS/GyGiuQQPw0RibIfO7KDbew7QfbnAq2RUYfsISuaE=</Modulus><Exponent>AQAB</Exponent><P>/YkeG+JrCnDCCopw99a8kElQfV1cfMHbDNhRNPLhbDYuWYGJVwy44zifS4Pf/Ow7hoxjhQPKn+SM98Bg5c90Lw==</P><Q>+Fvrwzpp4/in1FBbGJCtVsupoemBfu6iAEl4PFrkLnv9CxZjBBGsAzRalqxR/L6VEgi8CjzEDAMhWEAMwm+rLw==</Q><DP>mIPjmOKbgaxTDgQ58N08kY+I2+FNwb7cBt4Z+8Af2vi86RsDg1oj3owxRzwNghiZtla9h0bAnW7fXipcH+KsAQ==</DP><DQ>SMpoJygPsQlH5ypGCMUeiLnYVeQiWBmI0Zy26Lma9yTP3VgKXT7ws1+8zTxkKzeQaWC1/CojK4IfW8Yp9zLTcQ==</DQ><InverseQ>H3vT63EIIxjaanjyCbMNY+BRqGuNZxOYaTmwdVRZJJxahx/pDaQ54gQaG8jckazAACV6rfbpPJXCKZTqmIpgAA==</InverseQ><D>WXxTMnnE5jfeINEFlJROvtpxFXzxjSquYdES976zp0JDpGmRZe+NdlCO36V6QtussWpCZHCe8g7pT8mTB9jjV2FgRg2PzqTFZSk83RAplx6fpGfz4FvGrHpFbeF4lUgx1AfmxXI9q2Qlig+nOiVs+qOAwTIUz64UjaPlDJury00=</D></RSAKeyValue>" 
#endif
			}},
			{"8", new RsaKey(){ 
				PublicKey= "<RSAKeyValue><Modulus>q7jwQ+niQuG1wfdTdfcQTbG+Va2btkrzgehvvQzPXSxAZ0vnM5BnzCbpeNzLLQIEEjIZhPsaLpQvGYgvh0tZMi1jP4kpQ6D8hqWrNkhP1CNnHhouDG552SpreoeTmKjNcQJIjqW56coD/IfoFrEdsB2b3RX+38GILQyHiO85hps=</Modulus><Exponent>AQAB</Exponent></RSAKeyValue>",
#if LicenseTool
				PrivateKey= "<RSAKeyValue><Modulus>q7jwQ+niQuG1wfdTdfcQTbG+Va2btkrzgehvvQzPXSxAZ0vnM5BnzCbpeNzLLQIEEjIZhPsaLpQvGYgvh0tZMi1jP4kpQ6D8hqWrNkhP1CNnHhouDG552SpreoeTmKjNcQJIjqW56coD/IfoFrEdsB2b3RX+38GILQyHiO85hps=</Modulus><Exponent>AQAB</Exponent><P>0i9il/X0t6ce+u+UOX/hHgjlAN84EWxPQy0SmvDrlJcXS2fKLe5/cy/iEGo5iphlrGyU8gcMCC3ijPLoMm4YGw==</P><Q>0SdIiQcesvFiSQjsk2OSDHPTeXB9CnK7MXbF6r1TKtwBdL61axn/0/f2J5I80HsUC3/GJVakD3mjleHyF/YzgQ==</Q><DP>xqLhmptnWukl7iDdBDpGlgvx4JEUe+LQRbwjCSsGeLcR3MGJ4T60VDe8fabmGoVMEVfMq67dQB2dLXei2YYSYQ==</DP><DQ>fvtXqYH+HGw63NajM1TXfeHweaFW+Tnw9rYCsWLilHngFG4YnP66+IlqFFpPu8+NuvGHbnHsx0x1ifEU14FtgQ==</DQ><InverseQ>lVdw3kQZ5c33sXp6Utv8EXz7LFB6cGh2TB3V/is1LwABkCsS9+KXeH4HWFUQaKh2YPAXkuEBF921c97bf7BHEQ==</InverseQ><D>KrpqtktU7FVjN29GCh0+pkZq2MDI9PPzYUinvP+Z/VIMeGdvkOoGmcq430T5HSraPnjO1sLwQdTHX2/+e4ipGaXP+YFuNVTjUF996yBZ2Es3b6/2z/2S3YpnsQ0Nt3CT9VAHT31WC+2hvztPoualxeFJaWwlSUnYx09xEXmcQQE=</D></RSAKeyValue>" 
#endif
			}},
			{"9", new RsaKey(){ 
				PublicKey= "<RSAKeyValue><Modulus>tXE2eMPcrCFnMV1aGIDz41ZKpX6SMbySsR6CAX/HRou7rJwpFXwbp/arOpWgXE34r6bskzuYlEQlpvu+sVpgfy0sWCuN2eBZHCWW/u6A9b7CgnBfW+L6qm7+dgBBWhJmLoKl7KuagP+P/DIG29wE4V2lvxsteBXBcjHPIBF15G0=</Modulus><Exponent>AQAB</Exponent></RSAKeyValue>",
#if LicenseTool
				PrivateKey= "<RSAKeyValue><Modulus>tXE2eMPcrCFnMV1aGIDz41ZKpX6SMbySsR6CAX/HRou7rJwpFXwbp/arOpWgXE34r6bskzuYlEQlpvu+sVpgfy0sWCuN2eBZHCWW/u6A9b7CgnBfW+L6qm7+dgBBWhJmLoKl7KuagP+P/DIG29wE4V2lvxsteBXBcjHPIBF15G0=</Modulus><Exponent>AQAB</Exponent><P>5Mu1TukrTdA0sMUBsFF1+bz5r/rXPadiJQ9cubJvLaUM1yIlIKT27IYhfkD4AQWyu5WBAC5Ak0V4yUPhk/V/sQ==</P><Q>ywQdq4HuUYt5fwLGHL6dMnQDDYK99qIZrJhS/1tkvm7W9Lyo3lskcAF16WTEsg8kRo6yCixe6QuZ3vbloaT7fQ==</Q><DP>y3NNnYcAJ+ieAhadYtF9S/88NL2kysFeJ8BaXxKJhJhBK8jEJRwsKrU3cVKKdjY/8kiGdseqSos7VhWTsQNccQ==</DP><DQ>D3kh1ceSZW6u9Oo+NUSl8Il9DhOP7PNP56K4eLP7irQh+AdFC6WAsnS6Cu7eOwACcMpBcZsOJM74jFDfEaHkdQ==</DQ><InverseQ>XcVDTN2MMAeDIc6Amh+UxUK1XDyndVRojfZkkbp4ncGPyJGzIGWMGbqgwXwOxOvx/NYe5+6YouwKTS3oi84k7g==</InverseQ><D>PC94nSiDC4ns+//2QciMne1ix3Crt9nQYvY2s56aEHmLYF9QdXL8jYOSGtXFIrkuc72QHeqX2x6byr8Y6yWKejXaOcgpudIU/qOZ/7SVCGML6GiE4e3Skh7YyN6XZblSdLwaYr9oW6xvvVcwTjC7JwRhfX+bIQQMtXQihDMxncE=</D></RSAKeyValue>" 
#endif
			}},
		};
	}

	class RsaKey
	{
		public string PrivateKey { get; set; }
		public string PublicKey { get; set; }
	}
}
