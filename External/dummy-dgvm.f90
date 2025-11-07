!   SEIB-DGVMとSEIB-Explorer上を連携させるための制御プログラム
!   csvファイルを通じてUnity/Fortran間の通信を実行する
!   
!   By Hisashi SATO (JAMSTEC) 2025/Sep/11
!   
program dummy_dgvm
   implicit none
   
!___________ 固定パラメーター設定 ___________ 
   !シミュレーション年数
   integer,parameter:: sim_years=10
   
   ![For SEIB-Explorer] Unityとの通信に用いるファイル・フォルダ
   !なお、トップフォルダは、SEIB-Explorerインストールフォルダ直下の/External
   character(len=256),parameter:: FileName_Param = "input/params.csv" !入力パラメーターファイル
   character(len=256),parameter:: FileName_Stop  = "control/stop.txt" !停止信号ファイル
   character(len=256),parameter:: PathName_Out   = "output/"          !出力ファイル書き出し先
   
!___________ 変数設定 ___________ 
   ![For SEIB-Explorer] Unity出力のパラメーターファイルから読み出す変数
   real					:: input_ModAirTemp	!気温バイアス
   real					:: input_ModPrecip	!降水量バイアス
   real					:: input_ModCO2     !CO2濃度
   character(len=100)	:: Input_SiteName   !シミュレーションサイト名称
   character(len=100)	:: Input_FNout      !結果出力ファイル名（拡張子無し）
   
   ![For SEIB-Explorer] IO制御に用いる変数
   character(len=128) :: line
   integer :: ios
   logical :: istop
   
   !その他の変数
   integer :: year
   
!★___________ [For SEIB-Explorer] パラメータファイルの読み込み ___________ 
open(unit=10, file=FileName_Param, status='old', action='read', iostat=ios)
   if (ios /= 0) then
     print *, "[Fortran] 入力ファイルのオープンに失敗: ", trim(FileName_Param)
     stop
   end if
   
   ! 1行目（ヘッダー）を読み飛ばす
   read(10, '(A)', iostat=ios) line
   if (ios /= 0) then
     print *, "[Fortran] ヘッダー読み飛ばしに失敗"
     stop
   end if
   
   ! 2行目（値）を読む
   read(10, *, iostat=ios) input_ModAirTemp, input_ModPrecip, input_ModCO2, Input_SiteName, Input_FNout
close(10)
   
   if (ios /= 0) then
     print *, "[Fortran] 値読み込み失敗"
     stop
   end if
   
   print *, "[Fortran] パラメータ読み込み完了: ", sim_years, "年分シミュレーションを実行"
   
!___________ シミュレーションループ ___________ 
  do year = 1, sim_years
     !★[For SEIB-Explorer] 毎年、中断の指令が入っていないか確認する
    inquire(file=FileName_Stop, exist=istop)
    if (istop) then
      print *, "[Fortran] stop.txt が検出されました。中断します。"
      exit
    end if
	
    !少し待つ　(これ、windows環境でのみ可能)
    call sleep_ms(3000)
	
    !このシミュレーション年の結果ファイルを出力する
    !Unity側は存在するファイル名のみを参照、中身は見ない（いずれ変更する）
    call output_year_csv(year, 1000.0, 5.5, input_ModAirTemp, 1500.0, input_ModCO2)
    print *, "[Fortran] ", year, "年の結果を書き出しました。"
  end do

  print *, "[Fortran] シミュレーション終了しました。"









contains

!___________ 1年分の結果をcsvファイルに出力する ___________ 
subroutine output_year_csv(year, npp, lai, temp, precip, co2)
   integer, intent(in) :: year
   real, intent(in) :: npp, lai, temp, precip, co2
   character(len=256) :: fname
   integer :: u
   
!  write(fname, '(A,I0,A)') "output/year", year, ".csv" !ファイル名を生成
   write(fname, '(A,I0,A)') trim(PathName_Out)//"year", year, ".csv" !ファイル名を生成
open(newunit=u, file=fname, status="replace", action="write", iostat=ios)
   if (ios /= 0) then
     print *, "[Fortran] ファイル出力失敗: ", fname
     return
   end if
   
   !ヘッダー書き出し
   write(u, '(A)') "year,npp,lai,temperature,precipitation,co2"
   
   !値書き出し
   write(u, '(I0,",",F6.2,",",F5.2,",",F5.2,",",F6.2,",",F6.2)') &
       year, npp, lai, temp, precip, co2
   
   close(u)
end subroutine output_year_csv


!___________ 少し待つ (Windows用のSleep関数を使用)___________ 
  subroutine sleep_ms(ms)
    ! Windows用のSleep関数
    use iso_c_binding
    implicit none
    integer(c_int), value :: ms
    interface
      subroutine Sleep(millisec) bind(c, name="Sleep")
        import :: c_int
        integer(c_int), value :: millisec
      end subroutine Sleep
    end interface
    call Sleep(ms)
  end subroutine sleep_ms

end program dummy_dgvm
